using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace NtoLib.Recipes.MbeTable
{
    internal class PLC_Communication
    {
        private ushort _modbus_transactionID;

        private const byte CODE_ReadMultipleRegisters = 3;
        private const byte CODE_WriteMultipleRegisters = 16;

        private const ushort STATE_IDLE = 1;
        private const ushort STATE_WRITING_ALLOWED = 2;
        private const ushort STATE_WRITING_BLOCKED = 3;

        private const ushort CMD_WRITING_NOT_ACTIVE = 1;
        private const ushort CMD_WRITING_REQUEST = 2;

        public bool WriteRecipeToPlc(List<RecipeLine> recipe, CommunicationSettings settings)
        {
            TcpClient tcpClient = new TcpClient();
            try
            {
                tcpClient.ReceiveTimeout = (int)settings._Timeout;
                tcpClient.SendTimeout = (int)settings._Timeout;
                IPAddress iPAddress = FormIpAddress(settings);

                IAsyncResult asyncResult = tcpClient.BeginConnect(iPAddress, (int)settings._Port, (AsyncCallback)null, (object)null);
                WaitHandle asyncWaitHandle = asyncResult.AsyncWaitHandle;
                try
                {
                    if (!asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds((double)settings._Timeout / 1000.0), false))
                    {
                        tcpClient.Close();
                        throw new TimeoutException();
                    }
                    tcpClient.EndConnect(asyncResult);
                }
                finally
                {
                    asyncWaitHandle.Close();
                }
                NetworkStream stream = tcpClient.GetStream();

                SendWritingRequest(settings, stream, CMD_WRITING_REQUEST);

                bool isWriteAllowed = ReadPermissionToWrite(settings, stream);
                if (!isWriteAllowed)
                    throw new Exception("запись заблокирована контроллером");
                
                SendRecipeLength(settings, stream, 0);


                // Initialize arrays
                int num1 = 0;
                int index1 = 0;
                ushort[] floatArray = new ushort[recipe.Count * settings._float_colum_num * 2];
                ushort[] intArray = new ushort[recipe.Count * settings._int_colum_num];
                ushort[] boolArray = new ushort[recipe.Count * settings._bool_colum_num / 16 + (recipe.Count * settings._bool_colum_num % 16 > 0 ? 1 : 0)];

                for (int index2 = 0; index2 < boolArray.Length; ++index2)
                    boolArray[index2] = (ushort)0;


                // Fill data to arrays
                int num2 = 0;
                foreach (RecipeLine recipeLine in recipe)
                {
                    intArray[index1] = (ushort)recipeLine.GetCells[0].UIntValue;
                    ++index1;

                    intArray[index1] = (ushort)recipeLine.GetCells[1].UIntValue;
                    ++index1;

                    floatArray[num1 * 2] = (ushort)(recipeLine.GetCells[2].UIntValue & (uint)ushort.MaxValue);
                    floatArray[num1 * 2 + 1] = (ushort)(recipeLine.GetCells[2].UIntValue >> 16 & (uint)ushort.MaxValue);
                    ++num1;

                    floatArray[num1 * 2] = (ushort)(recipeLine.GetCells[3].UIntValue & (uint)ushort.MaxValue);
                    floatArray[num1 * 2 + 1] = (ushort)(recipeLine.GetCells[3].UIntValue >> 16 & (uint)ushort.MaxValue);
                    ++num1;
                    /*
                    foreach (TCell cell in recipeLine.GetCells)
                    {
                        if (cell.Type == CellType._int || cell.Type == CellType._enum)
                        {
                            intArray[index1] = (ushort)cell.UIntValue;
                            ++index1;
                        }
                        if (cell.Type == CellType._float || cell.Type == CellType._floatPercent || cell.Type == CellType._floatPowerSpeed ||
                            cell.Type == CellType._floatSecond || cell.Type == CellType._floatTemp || cell.Type == CellType._floatTempSpeed)
                        {
                            floatArray[num1 * 2] = (ushort)(cell.UIntValue & (uint)ushort.MaxValue);
                            floatArray[num1 * 2 + 1] = (ushort)(cell.UIntValue >> 16 & (uint)ushort.MaxValue);
                            ++num1;
                        }
                        if (cell.Type == CellType._bool)
                        {
                            boolArray[num2 / 16] += (ushort)(cell.UIntValue << num2 % 16);
                            ++num2;
                        }
                    }*/
                }


                // Send INT data
                ushort[] intDataToSend;
                for (int index3 = 0; index3 < intArray.Length; index3 += intDataToSend.Length)
                {
                    intDataToSend = new ushort[intArray.Length - index3 > 100 ? 100 : intArray.Length - index3];
                    for (int index4 = 0; index4 < intDataToSend.Length; ++index4)
                        intDataToSend[index4] = intArray[index3 + index4];
                    this.WriteModbusData(stream, (uint)((ulong)settings._IntBaseAddr + (ulong)index3), intDataToSend);
                }


                // Send FLOAT data
                ushort[] floatDataToSend;
                for (int index5 = 0; index5 < floatArray.Length; index5 += floatDataToSend.Length)
                {
                    floatDataToSend = new ushort[floatArray.Length - index5 > 100 ? 100 : floatArray.Length - index5];
                    for (int index6 = 0; index6 < floatDataToSend.Length; ++index6)
                        floatDataToSend[index6] = floatArray[index5 + index6];
                    this.WriteModbusData(stream, (uint)((ulong)settings._FloatBaseAddr + (ulong)index5), floatDataToSend);
                }


                // Send BOOL data
                ushort[] boolDataToSEnd;
                for (int index7 = 0; index7 < boolArray.Length; index7 += boolDataToSEnd.Length)
                {
                    boolDataToSEnd = new ushort[boolArray.Length - index7 > 100 ? 100 : boolArray.Length - index7];
                    for (int index8 = 0; index8 < boolDataToSEnd.Length; ++index8)
                        boolDataToSEnd[index8] = boolArray[index7 + index8];
                    this.WriteModbusData(stream, (uint)((ulong)settings._BoolBaseAddr + (ulong)index7), boolDataToSEnd);
                }

                SendRecipeLength(settings, stream, (ushort)recipe.Count);
                SendWritingRequest(settings, stream, CMD_WRITING_NOT_ACTIVE);
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                tcpClient.Close();
            }
            return true;
        }


        public List<RecipeLine> LoadRecipeFromPlc(CommunicationSettings settings)
        {
            TcpClient tcpClient = new TcpClient();
            try
            {
                tcpClient.ReceiveTimeout = (int)settings._Timeout;
                tcpClient.SendTimeout = (int)settings._Timeout;

                IPAddress iPAddress = FormIpAddress(settings);

                IAsyncResult asyncResult = tcpClient.BeginConnect(iPAddress, (int)settings._Port, (AsyncCallback)null, (object)null);
                WaitHandle asyncWaitHandle = asyncResult.AsyncWaitHandle;
                try
                {
                    if (!asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds((double)settings._Timeout / 1000.0), false))
                    {
                        tcpClient.Close();
                        throw new TimeoutException();
                    }
                    tcpClient.EndConnect(asyncResult);
                }
                finally
                {
                    asyncWaitHandle.Close();
                }

                List<RecipeLine> data = ReadDataFromPlc(settings, tcpClient);

                

                return data;
            }
            catch (Exception)
            {
                
                return null;
            }
            finally
            {
                tcpClient.Close();
            }
        }
        private List<RecipeLine> ReadDataFromPlc(CommunicationSettings settings, TcpClient tcpClient)
        {
            NetworkStream stream = tcpClient.GetStream();

            // Read control data
            ushort[] controlData = this.ReadModbusData(stream, settings._ControlBaseAddr, 3U);
            ushort capacity = controlData[0] == (ushort)1 || controlData[0] == (ushort)3 ? controlData[2] : throw new Exception("контроллер не готов к чтению");
            List<RecipeLine> data = new List<RecipeLine>((int)capacity);
            if (capacity > (ushort)0)
            {
                // Read INT data from PLC
                ushort[] intData = new ushort[(int)capacity * settings._int_colum_num];
                ushort[] rawIntData;
                for (int index1 = 0; index1 < intData.Length; index1 += rawIntData.Length)
                {
                    rawIntData = this.ReadModbusData(stream, (uint)((ulong)settings._IntBaseAddr + (ulong)index1), intData.Length - index1 > 100 ? 100U : (uint)(intData.Length - index1));
                    for (int index2 = 0; index2 < rawIntData.Length; ++index2)
                        intData[index1 + index2] = rawIntData[index2];
                }


                // Read FLOAT data from PLC
                ushort[] floatData = new ushort[(int)capacity * settings._float_colum_num * 2];
                ushort[] rawFloatData;
                for (int index1 = 0; index1 < floatData.Length; index1 += rawFloatData.Length)
                {
                    rawFloatData = this.ReadModbusData(stream, (uint)((ulong)settings._FloatBaseAddr + (ulong)index1), floatData.Length - index1 > 100 ? 100U : (uint)(floatData.Length - index1));
                    for (int index2 = 0; index2 < rawFloatData.Length; ++index2)
                        floatData[index1 + index2] = rawFloatData[index2];
                }


                // Read BOOL data from PLC
                ushort[] boolData = new ushort[(int)capacity * settings._bool_colum_num / 16 + ((int)capacity * settings._bool_colum_num % 16 > 0 ? 1 : 0)];
                ushort[] rawBoolData;
                for (int index1 = 0; index1 < boolData.Length; index1 += rawBoolData.Length)
                {
                    rawBoolData = this.ReadModbusData(stream, (uint)((ulong)settings._BoolBaseAddr + (ulong)index1), boolData.Length - index1 > 100 ? 100U : (uint)(boolData.Length - index1));
                    for (int index2 = 0; index2 < rawBoolData.Length; ++index2)
                        boolData[index1 + index2] = rawBoolData[index2];
                }

                RecipeLineFactory factory = new RecipeLineFactory();

                for (int index = 0; index < (int)capacity; ++index)
                    data.Add(factory.NewLine(intData, floatData, boolData, index));
            }
            return data;
        }

        private ushort[] ReadModbusData(NetworkStream cs, uint startAddress, uint registerCount)
        {
            if (registerCount > 100U)
                throw new Exception("Чтение за раз не более " + 100.ToString());
            if (registerCount < 1U)
                throw new Exception("чтение нулевой длины");
            byte[] numArray1 = new byte[12];
            int offset = 0;
            ++this._modbus_transactionID;
            BufferActions.WriteWordToBuffer(numArray1, ref offset, this._modbus_transactionID);
            BufferActions.WriteWordToBuffer(numArray1, ref offset, (ushort)0);
            BufferActions.WriteWordToBuffer(numArray1, ref offset, (ushort)6);
            BufferActions.WriteByteToBuffer(numArray1, ref offset, (byte)0);
            BufferActions.WriteByteToBuffer(numArray1, ref offset, CODE_ReadMultipleRegisters);  // operation code
            BufferActions.WriteWordToBuffer(numArray1, ref offset, (ushort)startAddress);
            BufferActions.WriteWordToBuffer(numArray1, ref offset, (ushort)registerCount);
            cs.Write(numArray1, 0, offset);
            byte[] numArray2 = new byte[(int)(IntPtr)(uint)(9 + (int)registerCount * 2)];
            int num = cs.Read(numArray2, 0, numArray2.Length);
            if (num <= 0)
                throw new Exception("No response from controller");
            if (num < numArray2.Length)
                throw new Exception("Resp lenght error");
            offset = 0;
            if ((int)BufferActions.ReadDWordFromBuffer(numArray2, ref offset) != (int)this._modbus_transactionID)
                throw new Exception("Modbus Wrong transaction ID");
            if (BufferActions.ReadDWordFromBuffer(numArray2, ref offset) != (ushort)0)
                throw new Exception("Modbus Wrong field 0");
            if ((int)BufferActions.ReadDWordFromBuffer(numArray2, ref offset) != 3 + (int)registerCount * 2)
                throw new Exception("Modbus Wrong field Lenght");
            if (BufferActions.ReadByteFromBuffer(numArray2, ref offset) != (byte)0)
                throw new Exception("Modbus Wrong field dev addr");
            if (BufferActions.ReadByteFromBuffer(numArray2, ref offset) != (byte)3)
                throw new Exception("Modbus Wrong field fun");
            if ((int)BufferActions.ReadByteFromBuffer(numArray2, ref offset) != (int)registerCount * 2)
                throw new Exception("Modbus Wrong field addr");
            ushort[] numArray3 = new ushort[(int)(IntPtr)registerCount];
            for (int index = 0; (long)index < (long)registerCount; ++index)
                numArray3[index] = BufferActions.ReadDWordFromBuffer(numArray2, ref offset);
            return numArray3;
        }
        private void WriteModbusData(NetworkStream cs, uint startAddress, ushort[] data)
        {
            if (data == null)
                throw new Exception("пустой буфер передачи");
            if (data.Length > 100)
                throw new Exception("Передача за раз не более " + 100.ToString());
            byte[] numArray1 = data.Length >= 1 ? new byte[data.Length * 2 + 13] : throw new Exception("передача нулевой длины");
            int offset = 0;
            ++this._modbus_transactionID;
            BufferActions.WriteWordToBuffer(numArray1, ref offset, this._modbus_transactionID);
            BufferActions.WriteWordToBuffer(numArray1, ref offset, (ushort)0);
            BufferActions.WriteWordToBuffer(numArray1, ref offset, (ushort)(data.Length * 2 + 7));
            BufferActions.WriteByteToBuffer(numArray1, ref offset, (byte)0);
            BufferActions.WriteByteToBuffer(numArray1, ref offset, CODE_WriteMultipleRegisters);
            BufferActions.WriteWordToBuffer(numArray1, ref offset, (ushort)startAddress);
            BufferActions.WriteWordToBuffer(numArray1, ref offset, (ushort)data.Length);
            BufferActions.WriteByteToBuffer(numArray1, ref offset, (byte)(data.Length * 2));
            for (int index = 0; index < data.Length; ++index)
                BufferActions.WriteWordToBuffer(numArray1, ref offset, data[index]);
            cs.Write(numArray1, 0, offset);

            // Read response
            byte[] responseData = new byte[300];
            int bytesReceived = cs.Read(responseData, 0, 300);

            // Check response
            if (bytesReceived <= 0)
                throw new Exception("No response from controller");
            if (bytesReceived < 12)
                throw new Exception("Resp lenght error");
            offset = 0;
            if ((int)BufferActions.ReadDWordFromBuffer(responseData, ref offset) != (int)this._modbus_transactionID)
                throw new Exception("Modbus Wrong transaction ID");

            if (BufferActions.ReadDWordFromBuffer(responseData, ref offset) != (ushort)0)
                throw new Exception("Modbus Wrong field 0");

            if (BufferActions.ReadDWordFromBuffer(responseData, ref offset) != (ushort)6)
                throw new Exception("Modbus Wrong field Lenght");

            if (BufferActions.ReadByteFromBuffer(responseData, ref offset) != (byte)0)
                throw new Exception("Modbus Wrong modbus addrress");

            byte receivedOperationCode = BufferActions.ReadByteFromBuffer(responseData, ref offset);
            if (receivedOperationCode != (byte)16)
                throw new Exception($"Modbus write response: Wrong operation code (Expected: 16, Received: {receivedOperationCode})");

            int receivedStartAddress = (int)BufferActions.ReadDWordFromBuffer(responseData, ref offset);
            if (receivedStartAddress != (int)startAddress)
                throw new Exception($"Modbus write response: Wrong start address (Expected: {startAddress}, Received: {receivedStartAddress})");

            int receivedLength = (int)BufferActions.ReadDWordFromBuffer(responseData, ref offset);
            if (receivedLength != (int)(ushort)data.Length)
                throw new Exception($"Modbus write response: Wrong length (Expected: {data.Length}, Received: {receivedLength})");
        }

        private IPAddress FormIpAddress(CommunicationSettings settings)
        {
            return new IPAddress((long)settings._IP1 & (long)byte.MaxValue |
                                ((long)settings._IP2 & (long)byte.MaxValue) << 8 |
                                ((long)settings._IP3 & (long)byte.MaxValue) << 16 |
                                ((long)settings._IP4 & (long)byte.MaxValue) << 24);
        }

        private void SendWritingRequest(CommunicationSettings settings, NetworkStream stream, ushort command)
        {
            ushort[] controlData = new ushort[1] { command };
            this.WriteModbusData(stream, settings._ControlBaseAddr + 1U, controlData);
        }
        private void SendRecipeLength(CommunicationSettings settings, NetworkStream stream, int length)
        {
            ushort[] recipeLengthData = new ushort[1] { (ushort)length };
            WriteModbusData(stream, settings._ControlBaseAddr + 2U, recipeLengthData);
        }
        private bool ReadPermissionToWrite(CommunicationSettings settings, NetworkStream stream)
        {
            ushort[] controlData = ReadModbusData(stream, settings._ControlBaseAddr, 1U);
            return controlData[0] == STATE_WRITING_ALLOWED;
        }
    }
}