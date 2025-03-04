using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NtoLib.Recipes.MbeTable.RecipeLines;

namespace NtoLib.Recipes.MbeTable.PLC
{
    internal class PlcCommunication
    {
        private ushort _modbusTransactionId;

        private const byte CodeReadMultipleRegisters = 3;
        private const byte CodeWriteMultipleRegisters = 16;

        private const ushort StateIdle = 1;
        private const ushort StateWritingAllowed = 2;
        private const ushort StateWritingBlocked = 3;

        private const ushort CmdWritingNotActive = 1;
        private const ushort CmdWritingRequest = 2;

        public bool WriteRecipeToPlc(List<RecipeLine> recipe, CommunicationSettings settings)
        {
            using TcpClient tcpClient = new();
            try
            {
                ConfigureTcpClient(tcpClient, settings);
                using var stream = ConnectToPlc(tcpClient, settings);

                RequestWritePermission(settings, stream);
                WriteRecipeData(settings, stream, recipe);
                CompleteWriteProcess(settings, stream, recipe.Count);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void ConfigureTcpClient(TcpClient tcpClient, CommunicationSettings settings)
        {
            // Visual Pins data
            tcpClient.ReceiveTimeout = (int)settings.Timeout;
            tcpClient.SendTimeout = (int)settings.Timeout;
        }

        private NetworkStream ConnectToPlc(TcpClient tcpClient, CommunicationSettings settings)
        {
            var ipAddress = FormIpAddress(settings);
            var asyncResult = tcpClient.BeginConnect(ipAddress, (int)settings.Port, null, null);

            if (!asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(settings.Timeout / 1000.0), false))
                throw new TimeoutException();

            tcpClient.EndConnect(asyncResult);
            return tcpClient.GetStream();
        }

        private void RequestWritePermission(CommunicationSettings settings, NetworkStream stream)
        {
            SendWritingRequest(settings, stream, CmdWritingRequest);
            if (!ReadPermissionToWrite(settings, stream))
                throw new Exception("Запись заблокирована контроллером");

            SendRecipeLength(settings, stream, 0);
        }

        private void WriteRecipeData(CommunicationSettings settings, NetworkStream stream, List<RecipeLine> recipe)
        {
            var (intArray, floatArray, boolArray) = PrepareRecipeData(settings, recipe);
            WriteDataInChunks(stream, settings.IntBaseAddr, intArray);
            WriteDataInChunks(stream, settings.FloatBaseAddr, floatArray);
            WriteDataInChunks(stream, settings.BoolBaseAddr, boolArray);
        }

        private void CompleteWriteProcess(CommunicationSettings settings, NetworkStream stream, int recipeCount)
        {
            SendRecipeLength(settings, stream, (ushort)recipeCount);
            SendWritingRequest(settings, stream, CmdWritingNotActive);
        }

        private (ushort[], ushort[], ushort[]) PrepareRecipeData(CommunicationSettings settings,
            List<RecipeLine> recipe)
        {
            var floatArray = new ushort[recipe.Count * settings.FloatColumNum * 2];
            var intArray = new ushort[recipe.Count * settings.IntColumNum];
            var boolArray = new ushort[recipe.Count * settings.BoolColumNum / 16 +
                                       (recipe.Count * settings.BoolColumNum % 16 > 0 ? 1 : 0)];

            int floatIndex = 0, intIndex = 0;
            foreach (var line in recipe)
            {
                intArray[intIndex++] = (ushort)line.Cells[0].UIntValue;
                intArray[intIndex++] = (ushort)line.Cells[1].UIntValue;

                floatArray[floatIndex++] = (ushort)(line.Cells[2].UIntValue & ushort.MaxValue);
                floatArray[floatIndex++] = (ushort)(line.Cells[2].UIntValue >> 16);
                floatArray[floatIndex++] = (ushort)(line.Cells[3].UIntValue & ushort.MaxValue);
                floatArray[floatIndex++] = (ushort)(line.Cells[3].UIntValue >> 16);
            }

            return (intArray, floatArray, boolArray);
        }

        private void WriteDataInChunks(NetworkStream stream, uint baseAddr, ushort[] data)
        {
            for (var i = 0; i < data.Length; i += 100)
            {
                var chunkSize = Math.Min(100, data.Length - i);
                var chunk = new ushort[chunkSize];
                Array.Copy(data, i, chunk, 0, chunkSize);
                WriteModbusData(stream, baseAddr + (uint)i, chunk);
            }
        }

        public List<RecipeLine> LoadRecipeFromPlc(CommunicationSettings settings)
        {
            using TcpClient tcpClient = new();
            ConfigureTcpClient(tcpClient, settings);

            try
            {
                using var stream = ConnectToPlc(tcpClient, settings);
                return ReadRecipeData(settings, stream);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private List<RecipeLine> ReadRecipeData(CommunicationSettings settings, NetworkStream stream)
        {
            var controlData = ReadModbusData(stream, settings.ControlBaseAddr, 3U);
            if (controlData[0] != 1 && controlData[0] != 3)
                throw new Exception("Контроллер не готов к чтению");

            var capacity = controlData[2];
            var data = new List<RecipeLine>(capacity);
            if (capacity <= 0) return data;

            var intData = ReadPlcData(stream, settings.IntBaseAddr, capacity * settings.IntColumNum);
            var floatData = ReadPlcData(stream, settings.FloatBaseAddr, capacity * settings.FloatColumNum * 2);
            var boolData = ReadPlcData(stream, settings.BoolBaseAddr, capacity * settings.BoolColumNum / 16 +
                                                                      (capacity * settings.BoolColumNum % 16 > 0
                                                                          ? 1
                                                                          : 0));


            for (var i = 0; i < capacity; i++)
                data.Add(RecipeLineFactory.NewLine(intData, floatData, boolData, i));

            return data;
        }

        private ushort[] ReadPlcData(NetworkStream stream, uint baseAddr, int length)
        {
            var data = new ushort[length];
            for (var i = 0; i < length; i += 100)
            {
                var chunkSize = Math.Min(100, length - i);
                var chunk = ReadModbusData(stream, baseAddr + (uint)i, (uint)chunkSize);
                Array.Copy(chunk, 0, data, i, chunkSize);
            }

            return data;
        }

        private ushort[] ReadModbusData(NetworkStream networkStream, uint startAddress, uint registerCount)
        {
            if (registerCount > 100U)
                throw new Exception("Чтение за раз не более 100 строк");
            if (registerCount < 1U)
                throw new Exception("Чтение нулевой длины");

            var numArray1 = new byte[12];

            var offset = 0;

            _modbusTransactionId++;

            BufferActions.WriteWordToBuffer(numArray1, ref offset, this._modbusTransactionId);
            BufferActions.WriteWordToBuffer(numArray1, ref offset, 0);
            BufferActions.WriteWordToBuffer(numArray1, ref offset, 6);
            BufferActions.WriteByteToBuffer(numArray1, ref offset, 0);
            BufferActions.WriteByteToBuffer(numArray1, ref offset, CodeReadMultipleRegisters); // operation code
            BufferActions.WriteWordToBuffer(numArray1, ref offset, (ushort)startAddress);
            BufferActions.WriteWordToBuffer(numArray1, ref offset, (ushort)registerCount);

            networkStream.Write(numArray1, 0, offset);

            var numArray2 = new byte[(int)(IntPtr)(uint)(9 + (int)registerCount * 2)];

            var num = networkStream.Read(numArray2, 0, numArray2.Length);

            if (num <= 0)
                throw new Exception("No response from controller");

            if (num < numArray2.Length)
                throw new Exception("Resp lenght error");

            offset = 0;

            if (BufferActions.ReadDWordFromBuffer(numArray2, ref offset) != _modbusTransactionId)
                throw new Exception("Modbus Wrong transaction ID");

            if (BufferActions.ReadDWordFromBuffer(numArray2, ref offset) != 0)
                throw new Exception("Modbus Wrong field 0");

            if (BufferActions.ReadDWordFromBuffer(numArray2, ref offset) != 3 + (int)registerCount * 2)
                throw new Exception("Modbus Wrong field Lenght");

            if (BufferActions.ReadByteFromBuffer(numArray2, ref offset) != 0)
                throw new Exception("Modbus Wrong field dev addr");

            if (BufferActions.ReadByteFromBuffer(numArray2, ref offset) != 3)
                throw new Exception("Modbus Wrong field fun");

            if (BufferActions.ReadByteFromBuffer(numArray2, ref offset) != (int)registerCount * 2)
                throw new Exception("Modbus Wrong field addr");

            var numArray3 = new ushort[(int)(IntPtr)registerCount];

            for (var index = 0; index < registerCount; ++index)
                numArray3[index] = BufferActions.ReadDWordFromBuffer(numArray2, ref offset);
            return numArray3;
        }

        private void WriteModbusData(NetworkStream cs, uint startAddress, ushort[] data)
        {
            if (data == null)
                throw new Exception("пустой буфер передачи");
            if (data.Length > 100)
                throw new Exception("Передача за раз не более 100");

            var numArray1 = data.Length >= 1
                ? new byte[data.Length * 2 + 13]
                : throw new Exception("передача нулевой длины");

            var offset = 0;

            _modbusTransactionId++;

            BufferActions.WriteWordToBuffer(numArray1, ref offset, this._modbusTransactionId);
            BufferActions.WriteWordToBuffer(numArray1, ref offset, 0);
            BufferActions.WriteWordToBuffer(numArray1, ref offset, (ushort)(data.Length * 2 + 7));
            BufferActions.WriteByteToBuffer(numArray1, ref offset, 0);
            BufferActions.WriteByteToBuffer(numArray1, ref offset, CodeWriteMultipleRegisters);
            BufferActions.WriteWordToBuffer(numArray1, ref offset, (ushort)startAddress);
            BufferActions.WriteWordToBuffer(numArray1, ref offset, (ushort)data.Length);
            BufferActions.WriteByteToBuffer(numArray1, ref offset, (byte)(data.Length * 2));

            foreach (var t in data)
                BufferActions.WriteWordToBuffer(numArray1, ref offset, t);

            cs.Write(numArray1, 0, offset);

            // Read response
            var responseData = new byte[300];
            var bytesReceived = cs.Read(responseData, 0, 300);

            // Check response
            if (bytesReceived <= 0)
                throw new Exception("No response from controller");
            if (bytesReceived < 12)
                throw new Exception("Resp lenght error");
            offset = 0;

            if (BufferActions.ReadDWordFromBuffer(responseData, ref offset) != _modbusTransactionId)
                throw new Exception("Modbus Wrong transaction ID");

            if (BufferActions.ReadDWordFromBuffer(responseData, ref offset) != 0)
                throw new Exception("Modbus Wrong field 0");

            if (BufferActions.ReadDWordFromBuffer(responseData, ref offset) != 6)
                throw new Exception("Modbus Wrong field Lenght");

            if (BufferActions.ReadByteFromBuffer(responseData, ref offset) != 0)
                throw new Exception("Modbus Wrong modbus addrress");

            var receivedOperationCode = BufferActions.ReadByteFromBuffer(responseData, ref offset);
            if (receivedOperationCode != 16)
                throw new Exception(
                    $"Modbus write response: Wrong operation code (Expected: 16, Received: {receivedOperationCode})");

            var receivedStartAddress = (int)BufferActions.ReadDWordFromBuffer(responseData, ref offset);
            if (receivedStartAddress != (int)startAddress)
                throw new Exception(
                    $"Modbus write response: Wrong start address (Expected: {startAddress}, Received: {receivedStartAddress})");

            var receivedLength = (int)BufferActions.ReadDWordFromBuffer(responseData, ref offset);
            if (receivedLength != (ushort)data.Length)
                throw new Exception(
                    $"Modbus write response: Wrong length (Expected: {data.Length}, Received: {receivedLength})");
        }

        private IPAddress FormIpAddress(CommunicationSettings settings)
        {
            return new IPAddress(settings.Ip1 & (long)byte.MaxValue |
                                 (settings.Ip2 & (long)byte.MaxValue) << 8 |
                                 (settings.Ip3 & (long)byte.MaxValue) << 16 |
                                 (settings.Ip4 & (long)byte.MaxValue) << 24);
        }

        private void SendWritingRequest(CommunicationSettings settings, NetworkStream stream, ushort command)
        {
            var controlData = new ushort[] { command };
            WriteModbusData(stream, settings.ControlBaseAddr + 1U, controlData);
        }

        private void SendRecipeLength(CommunicationSettings settings, NetworkStream stream, int length)
        {
            var recipeLengthData = new ushort[] { (ushort)length };
            WriteModbusData(stream, settings.ControlBaseAddr + 2U, recipeLengthData);
        }

        private bool ReadPermissionToWrite(CommunicationSettings settings, NetworkStream stream)
        {
            var controlData = ReadModbusData(stream, settings.ControlBaseAddr, 1U);
            return controlData[0] == StateWritingAllowed;
        }
    }
}