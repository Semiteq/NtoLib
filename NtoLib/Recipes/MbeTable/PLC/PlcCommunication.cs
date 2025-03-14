﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using EasyModbus;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.PLC
{
    internal class PlcCommunication
    {
        private const ushort StateIdle = 1;
        private const ushort StateWritingAllowed = 2;
        private const ushort StateWritingBlocked = 3;

        private const ushort CmdWritingNotActive = 1;
        private const ushort CmdWritingRequest = 2;

        private string _ip;

        public bool WriteRecipeToPlc(List<RecipeLine> recipe, CommunicationSettings settings)
        {
            try
            {
                _ip = $"{settings.Ip1}.{settings.Ip2}.{settings.Ip3}.{settings.Ip4}";
                var modbusClient = new ModbusClient(_ip, (int)settings.Port);
                modbusClient.Connect();

                RequestWritePermission(settings, modbusClient);
                WriteRecipeData(settings, modbusClient, recipe);
                CompleteWriteProcess(settings, modbusClient, recipe.Count);

                modbusClient.Disconnect();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private void RequestWritePermission(CommunicationSettings settings, ModbusClient modbusClient)
        {
            modbusClient.WriteSingleRegister((int)(settings.ControlBaseAddr + 1U), CmdWritingRequest);
            if (modbusClient.ReadHoldingRegisters((int)settings.ControlBaseAddr, 1)[0] != StateWritingAllowed)
            {
                Debug.WriteLine($"Controller is not ready for writing, state: {modbusClient.ReadHoldingRegisters((int)settings.ControlBaseAddr, 1)[0]}");
                StatusManager.WriteStatusMessage("Запись заблокирована контроллером", true);
            }
            modbusClient.WriteSingleRegister((int)(settings.ControlBaseAddr + 2U), 0);
        }

        private void WriteRecipeData(CommunicationSettings settings, ModbusClient modbusClient, List<RecipeLine> recipe)
        {
            var (intArray, floatArray, boolArray) = PrepareRecipeData(settings, recipe);

            if (intArray.Length > 0)
                modbusClient.WriteMultipleRegisters((int)settings.IntBaseAddr, intArray);

            if (floatArray.Length > 0)
                modbusClient.WriteMultipleRegisters((int)settings.FloatBaseAddr, floatArray);

            if (boolArray.Length > 0)
                modbusClient.WriteMultipleRegisters((int)settings.BoolBaseAddr, boolArray);
        }

        private void CompleteWriteProcess(CommunicationSettings settings, ModbusClient modbusClient, int recipeCount)
        {
            modbusClient.WriteSingleRegister((int)(settings.ControlBaseAddr + 2U), (ushort)recipeCount);
            modbusClient.WriteSingleRegister((int)(settings.ControlBaseAddr + 1U), CmdWritingNotActive);
        }

        private (int[], int[], int[]) PrepareRecipeData(CommunicationSettings settings, List<RecipeLine> recipe)
        {
            var floatArray = new int[recipe.Count * settings.FloatColumNum * 2];
            var intArray = new int[recipe.Count * settings.IntColumNum];
            var boolArray = new int[recipe.Count * settings.BoolColumNum / 16 +
                                    (recipe.Count * settings.BoolColumNum % 16 > 0 ? 1 : 0)];

            int floatIndex = 0, intIndex = 0;
            foreach (var line in recipe)
            {
                intArray[intIndex++] = (ushort)line.Cells[0].IntValue;
                intArray[intIndex++] = (ushort)line.Cells[1].IntValue;

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

        public List<RecipeLine> LoadRecipeFromPlc(CommunicationSettings settings)
        {
            _ip = $"{settings.Ip1}.{settings.Ip2}.{settings.Ip3}.{settings.Ip4}";
            var modbusClient = new ModbusClient(_ip, (int)settings.Port);
            modbusClient.Connect();

            try
            {
                return ReadRecipeData(settings, modbusClient);
            }
            catch (Exception)
            {
                Debug.WriteLine($"Error reading recipe from PLC {_ip}");
                return null;
            }
            finally
            {
                modbusClient.Disconnect();
            }
        }

        private List<RecipeLine> ReadRecipeData(CommunicationSettings settings, ModbusClient modbusClient)
        {
            var controlData = modbusClient.ReadHoldingRegisters((int)settings.ControlBaseAddr, 3);
            if (controlData[0] != StateIdle && controlData[0] != StateWritingBlocked)
            {
                Debug.WriteLine($"Controller is not ready for reading, state: {controlData[0]}");
                StatusManager.WriteStatusMessage("Контроллер не готов к чтению", true);
            }

            var capacity = controlData[2];
            var data = new List<RecipeLine>(capacity);
            if (capacity <= 0) return data;


            var intQuantity = capacity * settings.IntColumNum;
            var floatQuantity = capacity * settings.FloatColumNum * 2;
            var boolQuantity = capacity * settings.BoolColumNum / 16 + (capacity * settings.BoolColumNum % 16 > 0 ? 1 : 0);

            var intData = Array.Empty<int>();
            var floatData = Array.Empty<int>();
            var boolData = Array.Empty<int>();

            if (intQuantity > 0)
                intData = modbusClient.ReadHoldingRegisters((int)settings.IntBaseAddr, intQuantity);

            if (floatQuantity > 0)
                floatData = modbusClient.ReadHoldingRegisters((int)settings.FloatBaseAddr, floatQuantity);

            if (boolQuantity > 0)
                boolData = modbusClient.ReadHoldingRegisters((int)settings.BoolBaseAddr, boolQuantity);

            for (var i = 0; i < capacity; i++)
                data.Add(RecipeLineFactory.NewLine(intData, floatData, boolData, i));

            return data;
        }
    }
}