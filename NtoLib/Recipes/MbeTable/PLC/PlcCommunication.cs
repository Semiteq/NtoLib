#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using EasyModbus;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.PLC
{
    public interface IPlcCommunication
    {
        bool CheckConnection(CommunicationSettings settings);
        bool WriteRecipeToPlc(List<RecipeLine> recipe, CommunicationSettings settings);
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

        private readonly IStatusManager statusManager;

        public PlcCommunication(IStatusManager StatusManager)
        {
            statusManager = StatusManager;
        }

        public bool CheckConnection(CommunicationSettings settings)
        {
            var ip = BuildIpString(settings);
            var modbusClient = new ModbusClient(ip, (int)settings.Port);

            try
            {
                modbusClient.Connect();
                modbusClient.ReadHoldingRegisters((int)settings.ControlBaseAddr, 1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
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
                modbusClient.Connect();

                // Request permission for writing
                if (!RequestWritePermission(settings, modbusClient, statusManager))
                {
                    return false;
                }

                // Write recipe data to PLC
                if (!WriteRecipeData(settings, modbusClient, recipe, statusManager))
                {
                    return false;
                }

                // Complete writing process by informing PLC about new recipe count
                CompleteWriteProcess(settings, modbusClient, recipe.Count);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
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
                modbusClient.Connect();
                return ReadRecipeData(settings, modbusClient, statusManager);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading recipe from PLC {ip}: {ex.Message}");
                return null;
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

        // Request write permission from the controller
        private bool RequestWritePermission(CommunicationSettings settings, ModbusClient modbusClient, IStatusManager? statusManager)
        {
            modbusClient.WriteSingleRegister((int)(settings.ControlBaseAddr + 1U), CmdWritingRequest);

            var state = modbusClient.ReadHoldingRegisters((int)settings.ControlBaseAddr, 1)[0];
            if (state != StateWritingAllowed)
            {
                Debug.WriteLine($"Controller is not ready for writing, state: {state}");
                statusManager?.WriteStatusMessage("Запись заблокирована контроллером", true);
                return false;
            }

            // Clear previous recipe count
            modbusClient.WriteSingleRegister((int)(settings.ControlBaseAddr + 2U), 0);
            return true;
        }

        // Write recipe data to PLC with chunked register writing
        private bool WriteRecipeData(CommunicationSettings settings, ModbusClient modbusClient, List<RecipeLine> recipe, IStatusManager? statusManager)
        {
            var (intArray, floatArray, boolArray) = PrepareRecipeData(settings, recipe);

            if (intArray.Length > settings.IntAreaSize)
            {
                Debug.WriteLine($"Int area size exceeded: {intArray.Length} > {settings.IntAreaSize}");
                statusManager?.WriteStatusMessage("Превышен размер области Int в ПЛК", true);
                return false;
            }
            if (floatArray.Length > settings.FloatAreaSize)
            {
                Debug.WriteLine($"Float area size exceeded: {floatArray.Length} > {settings.FloatAreaSize}");
                statusManager?.WriteStatusMessage("Превышен размер области Float в ПЛК", true);
                return false;
            }
            if (boolArray.Length > settings.BoolAreaSize)
            {
                Debug.WriteLine($"Bool area size exceeded: {boolArray.Length} > {settings.BoolAreaSize}");
                statusManager?.WriteStatusMessage("Превышен размер области Bool в ПЛК", true);
                return false;
            }

            if (intArray.Length > 0)
                WriteRegistersChunked(modbusClient, (int)settings.IntBaseAddr, intArray);

            if (floatArray.Length > 0)
                WriteRegistersChunked(modbusClient, (int)settings.FloatBaseAddr, floatArray);

            if (boolArray.Length > 0)
                WriteRegistersChunked(modbusClient, (int)settings.BoolBaseAddr, boolArray);

            return true;
        }

        // Write registers in chunks to avoid exceeding Modbus request size
        private void WriteRegistersChunked(ModbusClient modbusClient, int baseAddress, int[] values)
        {
            var totalRegisters = values.Length;
            var currentIndex = 0;

            while (currentIndex < totalRegisters)
            {
                var chunkSize = Math.Min(MaxChunkSize, totalRegisters - currentIndex);
                var chunk = new int[chunkSize];
                Array.Copy(values, currentIndex, chunk, 0, chunkSize);

                modbusClient.WriteMultipleRegisters(baseAddress + currentIndex, chunk);
                currentIndex += chunkSize;
            }
        }

        // Complete the write process by setting recipe count and resetting write command
        private void CompleteWriteProcess(CommunicationSettings settings, ModbusClient modbusClient, int recipeCount)
        {
            modbusClient.WriteSingleRegister((int)(settings.ControlBaseAddr + 2U), (ushort)recipeCount);
            modbusClient.WriteSingleRegister((int)(settings.ControlBaseAddr + 1U), CmdWritingNotActive);
        }

        // Prepare data arrays from the recipe for writing to PLC
        private (int[] intArray, int[] floatArray, int[] boolArray) PrepareRecipeData(CommunicationSettings settings, List<RecipeLine> recipe)
        {
            // todo: boolArray remains empty, data preparation required.
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

        // Read recipe data from PLC, including int, float и bool arrays
        private List<RecipeLine> ReadRecipeData(CommunicationSettings settings, ModbusClient modbusClient, IStatusManager? statusManager)
        {
            var controlData = modbusClient.ReadHoldingRegisters((int)settings.ControlBaseAddr, 3);
            if (controlData[0] != StateIdle && controlData[0] != StateWritingBlocked)
            {
                Debug.WriteLine($"Controller is not ready for reading, state: {controlData[0]}");
                statusManager?.WriteStatusMessage("Контроллер не готов к чтению", true);
            }

            var capacity = controlData[2];
            var data = new List<RecipeLine>(capacity);
            if (capacity <= 0)
                return data;

            var intQuantity = capacity * settings.IntColumNum;
            var floatQuantity = capacity * settings.FloatColumNum * 2;
            var boolQuantity = capacity * settings.BoolColumNum / 16 + (capacity * settings.BoolColumNum % 16 > 0 ? 1 : 0);

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
                currentIndex += chunkSize;
            }

            return result;
        }
    }
}
