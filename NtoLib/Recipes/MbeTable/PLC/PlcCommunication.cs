﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using EasyModbus;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.PLC
{
    public interface IPlcCommunication
    {
        // Returns true if connection check succeeded, otherwise throws an exception
        bool CheckConnection(CommunicationSettings settings);
        // Returns true if recipe successfully written, otherwise throws an exception
        bool WriteRecipeToPlc(List<RecipeLine> recipe, CommunicationSettings settings);
        // Returns recipe lines if successful, otherwise throws an exception
        List<RecipeLine> LoadRecipeFromPlc(CommunicationSettings settings);
    }

    public class PlcCommunication : IPlcCommunication
    {
        // Controller states
        private const ushort StateIdle = 1;
        private const ushort StateWritingAllowed = 2;
        private const ushort StateWritingBlocked = 3;

        // Writing command codes
        private const ushort CmdWritingNotActive = 1;
        private const ushort CmdWritingRequest = 2;

        // Maximum registers per Modbus request chunk (calculation: 256 bytes / 2 - overhead)
        private const int MaxChunkSize = 123;

        public bool CheckConnection(CommunicationSettings settings)
        {
            var ip = BuildIpString(settings);
            var modbusClient = new ModbusClient(ip, (int)settings.Port);
            try
            {
                Debug.WriteLine($"[CheckConnection] Attempting connection to {ip}:{settings.Port}");
                modbusClient.Connect();
                // Try reading a single register to verify connection
                modbusClient.ReadHoldingRegisters((int)settings.ControlBaseAddr, 1);
                Debug.WriteLine("[CheckConnection] Connection successful.");
            }
            catch (Exception ex)
            {
                // Log error with stack trace and rethrow
                Debug.WriteLine($"[CheckConnection] Exception during connection: {ex}");
                throw;
            }
            finally
            {
                modbusClient.Disconnect();
            }
            return true;
        }

        public bool WriteRecipeToPlc(List<RecipeLine> recipe, CommunicationSettings settings)
        {
            var ip = BuildIpString(settings);
            var modbusClient = new ModbusClient(ip, (int)settings.Port);
            try
            {
                Debug.WriteLine($"[WriteRecipeToPlc] Connecting to PLC at {ip}:{settings.Port}");
                modbusClient.Connect();

                // Request permission for writing
                RequestWritePermission(settings, modbusClient);
                Debug.WriteLine("[WriteRecipeToPlc] Write permission granted.");

                // Write recipe data to PLC
                WriteRecipeData(settings, modbusClient, recipe);
                Debug.WriteLine("[WriteRecipeToPlc] Recipe data written successfully.");

                // Complete writing process by informing PLC about new recipe count
                CompleteWriteProcess(settings, modbusClient, recipe.Count);
                Debug.WriteLine("[WriteRecipeToPlc] Write process completed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WriteRecipeToPlc] Exception during write process: {ex}");
                throw;
            }
            finally
            {
                modbusClient.Disconnect();
            }
            return true;
        }

        public List<RecipeLine> LoadRecipeFromPlc(CommunicationSettings settings)
        {
            var ip = BuildIpString(settings);
            var modbusClient = new ModbusClient(ip, (int)settings.Port);
            try
            {
                Debug.WriteLine($"[LoadRecipeFromPlc] Connecting to PLC at {ip}:{settings.Port}");
                modbusClient.Connect();
                var recipeLines = ReadRecipeData(settings, modbusClient);
                Debug.WriteLine($"[LoadRecipeFromPlc] Loaded {recipeLines.Count} recipe lines.");
                return recipeLines;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadRecipeFromPlc] Exception during read process: {ex}");
                throw;
            }
            finally
            {
                modbusClient.Disconnect();
            }
        }

        // Helper method to build IP address string from settings
        private string BuildIpString(CommunicationSettings settings)
        {
            return $"{settings.Ip1}.{settings.Ip2}.{settings.Ip3}.{settings.Ip4}";
        }

        // Request write permission from the controller. Throws exception if not allowed.
        private void RequestWritePermission(CommunicationSettings settings, ModbusClient modbusClient)
        {
            const int maxAttempts = 5;
            const int retryDelay = 50;
            Debug.WriteLine("[RequestWritePermission] Requesting write permission.");

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    // Request write permission 
                    modbusClient.WriteSingleRegister((int)(settings.ControlBaseAddr + 1U), CmdWritingRequest);
                    Thread.Sleep(retryDelay * attempt);

                    // Check controller state to see if writing is allowed
                    var state = modbusClient.ReadHoldingRegisters((int)settings.ControlBaseAddr, 1)[0];

                    if (state == StateWritingAllowed)
                    {
                        // Clear previous recipe count if write permission granted
                        modbusClient.WriteSingleRegister((int)(settings.ControlBaseAddr + 2U), 0);
                        Debug.WriteLine("[RequestWritePermission] Write permission granted, previous recipe count cleared.");
                        return;
                    }

                    Debug.WriteLine($"[RequestWritePermission] Attempt {attempt + 1} failed, controller state: {state}");
                    Thread.Sleep(retryDelay);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[RequestWritePermission] Communication error on attempt {attempt + 1}: {ex.Message}");
                    if (attempt == maxAttempts - 1) throw;
                }
            }

            throw new Exception("[RequestWritePermission] Write permission denied: maximum attempts exceeded");
        }

        // Write recipe data to PLC with chunked register writing
        private void WriteRecipeData(CommunicationSettings settings, ModbusClient modbusClient, List<RecipeLine> recipe)
        {
            var (intArray, floatArray, boolArray) = PrepareRecipeData(settings, recipe);

            if (intArray.Length > settings.IntAreaSize)
                throw new Exception($"[WriteRecipeData] PLC Int area size exceeded: {intArray.Length} > {settings.IntAreaSize}");

            if (floatArray.Length > settings.FloatAreaSize)
                throw new Exception($"[WriteRecipeData] PLC Float area size exceeded: {floatArray.Length} > {settings.FloatAreaSize}");

            if (boolArray.Length > settings.BoolAreaSize)
                throw new Exception($"[WriteRecipeData] PLC Bool area size exceeded: {boolArray.Length} > {settings.BoolAreaSize}");

            if (intArray.Length > 0)
                TryWriteRegistersChunked(modbusClient, (int)settings.IntBaseAddr, intArray);

            if (floatArray.Length > 0)
                TryWriteRegistersChunked(modbusClient, (int)settings.FloatBaseAddr, floatArray);

            if (boolArray.Length > 0)
                TryWriteRegistersChunked(modbusClient, (int)settings.BoolBaseAddr, boolArray);
        }

        // Write registers in chunks to avoid exceeding Modbus request size
        private void TryWriteRegistersChunked(ModbusClient modbusClient, int baseAddress, int[] values)
        {
            try
            {
                var totalRegisters = values.Length;
                var currentIndex = 0;

                while (currentIndex < totalRegisters)
                {
                    var chunkSize = Math.Min(MaxChunkSize, totalRegisters - currentIndex);
                    var chunk = new int[chunkSize];
                    Array.Copy(values, currentIndex, chunk, 0, chunkSize);

                    modbusClient.WriteMultipleRegisters(baseAddress + currentIndex, chunk);
                    Debug.WriteLine($"[TryWriteRegistersChunked] Written chunk at address {baseAddress + currentIndex} with {chunkSize} registers.");
                    currentIndex += chunkSize;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"[TryWriteRegistersChunked] Error writing data in chunks: {ex.Message}");
            }
        }

        // Complete the write process by setting recipe count and resetting write command
        private void CompleteWriteProcess(CommunicationSettings settings, ModbusClient modbusClient, int recipeCount)
        {
            Debug.WriteLine("[CompleteWriteProcess] Completing write process.");
            modbusClient.WriteSingleRegister((int)(settings.ControlBaseAddr + 2U), (ushort)recipeCount);
            modbusClient.WriteSingleRegister((int)(settings.ControlBaseAddr + 1U), CmdWritingNotActive);
        }

        // Prepare data arrays from the recipe for writing to PLC
        private (int[] intArray, int[] floatArray, int[] boolArray) PrepareRecipeData(CommunicationSettings settings, List<RecipeLine> recipe)
        {
            // Note: boolArray remains empty; data preparation for boolean values should be implemented.
            var floatArray = new int[recipe.Count * settings.FloatColumNum * 2];
            var intArray = new int[recipe.Count * settings.IntColumNum];
            var boolArray = new int[recipe.Count * settings.BoolColumNum / 16 + (recipe.Count * settings.BoolColumNum % 16 > 0 ? 1 : 0)];

            var floatIndex = 0;
            var intIndex = 0;
            foreach (var line in recipe)
            {
                // Assuming first two cells are int values
                intArray[intIndex++] = (ushort)line.Cells[0].IntValue;
                intArray[intIndex++] = (ushort)line.Cells[1].IntValue;

                // Next cells are float values converted to two 16-bit values each
                var bytes2 = BitConverter.GetBytes(line.Cells[2].FloatValue);
                floatArray[floatIndex++] = BitConverter.ToUInt16(bytes2, 0);
                floatArray[floatIndex++] = BitConverter.ToUInt16(bytes2, 2);

                var bytes3 = BitConverter.GetBytes(line.Cells[3].FloatValue);
                floatArray[floatIndex++] = BitConverter.ToUInt16(bytes3, 0);
                floatArray[floatIndex++] = BitConverter.ToUInt16(bytes3, 2);

                var bytes4 = BitConverter.GetBytes(line.Cells[4].FloatValue);
                floatArray[floatIndex++] = BitConverter.ToUInt16(bytes4, 0);
                floatArray[floatIndex++] = BitConverter.ToUInt16(bytes4, 2);

                var bytes5 = BitConverter.GetBytes(line.Cells[5].FloatValue);
                floatArray[floatIndex++] = BitConverter.ToUInt16(bytes5, 0);
                floatArray[floatIndex++] = BitConverter.ToUInt16(bytes5, 2);
            }

            return (intArray, floatArray, boolArray);
        }

        // Read recipe data from PLC, including int, float and bool arrays
        private List<RecipeLine> ReadRecipeData(CommunicationSettings settings, ModbusClient modbusClient)
        {
            Debug.WriteLine("[ReadRecipeData] Reading control data.");
            var controlData = modbusClient.ReadHoldingRegisters((int)settings.ControlBaseAddr, 3);

            if (controlData[0] != StateIdle && controlData[0] != StateWritingBlocked)
                throw new Exception($"[ReadRecipeData] Controller not ready for reading, state: {controlData[0]}");

            var capacity = controlData[2];
            var data = new List<RecipeLine>(capacity);

            if (capacity <= 0)
                return data;

            var intQuantity = capacity * settings.IntColumNum;
            var floatQuantity = capacity * settings.FloatColumNum * 2;
            var boolQuantity = capacity * settings.BoolColumNum / 16 + (capacity * settings.BoolColumNum % 16 > 0 ? 1 : 0);

            Debug.WriteLine($"[ReadRecipeData] Reading {intQuantity} int, {floatQuantity} float and {boolQuantity} bool registers.");
            var intData = intQuantity > 0 ? ReadRegistersChunked(modbusClient, (int)settings.IntBaseAddr, intQuantity) : Array.Empty<int>();
            var floatData = floatQuantity > 0 ? ReadRegistersChunked(modbusClient, (int)settings.FloatBaseAddr, floatQuantity) : Array.Empty<int>();
            var boolData = boolQuantity > 0 ? ReadRegistersChunked(modbusClient, (int)settings.BoolBaseAddr, boolQuantity) : Array.Empty<int>();

            for (var i = 0; i < capacity; i++)
            {
                data.Add(RecipeLineFactory.NewLine(intData, floatData, boolData, i));
            }
            return data;
        }

        // Read registers in chunks
        private int[] ReadRegistersChunked(ModbusClient modbusClient, int baseAddress, int totalRegisters)
        {
            var result = new int[totalRegisters];
            var currentIndex = 0;

            while (currentIndex < totalRegisters)
            {
                var chunkSize = Math.Min(MaxChunkSize, totalRegisters - currentIndex);
                var chunk = modbusClient.ReadHoldingRegisters(baseAddress + currentIndex, chunkSize);
                Array.Copy(chunk, 0, result, currentIndex, chunkSize);
                Debug.WriteLine($"[ReadRegistersChunked] Read chunk from address {baseAddress + currentIndex} of size {chunkSize}.");
                currentIndex += chunkSize;
            }
            return result;
        }
    }
}
