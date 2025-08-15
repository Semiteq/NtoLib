#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using EasyModbus;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PlcCommunication;

/// <summary>
/// V1 Modbus communicator using EasyModbus and control registers handshake.
/// </summary>
public sealed class ModbusCommunicatorV1 : IModbusCommunicator
{
    private const ushort StateIdle = 1;
    private const ushort StateWritingAllowed = 2;
    private const ushort StateWritingBlocked = 3;

    private const ushort CmdWritingNotActive = 1;
    private const ushort CmdWritingRequest = 2;

    private const int MaxChunkSize = 123;

    private readonly IPlcRecipeMapper _mapper;
    private readonly ILogger _logger;

    public ModbusCommunicatorV1(IPlcRecipeMapper mapper, ILogger logger)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Verifies the connectivity to a PLC using the provided communication settings.
    /// </summary>
    /// <param name="settings">The communication settings, including IP address and port, used to establish the connection to the PLC.</param>
    /// <returns>True if the connection to the PLC is successful; otherwise, false.</returns>
    /// <exception cref="Exception">Thrown when an error occurs during connection establishment or communication with the PLC.</exception>
    public bool CheckConnection(PinDataManager.CommunicationSettings settings)
    {
        var ip = BuildIp(settings);
        var client = new ModbusClient(ip, (int)settings.Port);
        try
        {
            _logger.Log($"[CheckConnection] {ip}:{settings.Port}");
            client.Connect();
            client.ReadHoldingRegisters((int)settings.ControlBaseAddr, 1);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            Debug.WriteLine($"[CheckConnection] Exception: {ex}");
            throw;
        }
        finally
        {
            try { client.Disconnect(); } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// Writes the provided recipe to the PLC using the specified communication settings.
    /// </summary>
    /// <param name="recipe">The recipe represented as a list of steps to be written to the PLC.</param>
    /// <param name="settings">The communication settings used to establish a connection to the PLC for recipe transmission.</param>
    /// <returns>True if the recipe is successfully written to the PLC; otherwise, false.</returns>
    /// <exception cref="Exception">Thrown when an error occurs during connection establishment, communication, or data transmission to the PLC.</exception>
    public bool WriteRecipeToPlc(List<Step> recipe, PinDataManager.CommunicationSettings settings)
    {
        var ip = BuildIp(settings);
        var client = new ModbusClient(ip, (int)settings.Port);
        try
        {
            _logger.Log($"[WriteRecipeToPlc] Connect {ip}:{settings.Port}");
            client.Connect();

            RequestWritePermission(settings, client);

            var (intArray, floatArray, boolArray) = _mapper.ToRegisters(recipe);

            if (intArray.Length > settings.IntAreaSize)
                throw new Exception($"INT area exceeded: {intArray.Length} > {settings.IntAreaSize}");
            if (floatArray.Length > settings.FloatAreaSize)
                throw new Exception($"FLOAT area exceeded: {floatArray.Length} > {settings.FloatAreaSize}");
            if (boolArray.Length > settings.BoolAreaSize)
                throw new Exception($"BOOL area exceeded: {boolArray.Length} > {settings.BoolAreaSize}");

            if (intArray.Length > 0)
                WriteChunked(client, (int)settings.IntBaseAddr, intArray);
            if (floatArray.Length > 0)
                WriteChunked(client, (int)settings.FloatBaseAddr, floatArray);
            if (boolArray.Length > 0)
                WriteChunked(client, (int)settings.BoolBaseAddr, boolArray);

            CompleteWrite(settings, client, recipe.Count);
            _logger.Log("[WriteRecipeToPlc] Done");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            Debug.WriteLine($"[WriteRecipeToPlc] Exception: {ex}");
            throw;
        }
        finally
        {
            try { client.Disconnect(); } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// Loads a recipe from the PLC using the provided communication settings.
    /// </summary>
    /// <param name="settings">The communication settings used to establish a connection to the PLC and retrieve the recipe data.</param>
    /// <returns>A list of steps representing the recipe data retrieved from the PLC.</returns>
    /// <exception cref="Exception">Thrown when an error occurs during communication or data retrieval from the PLC.</exception>
    public List<Step> LoadRecipeFromPlc(PinDataManager.CommunicationSettings settings)
    {
        var ip = BuildIp(settings);
        var client = new ModbusClient(ip, (int)settings.Port);
        try
        {
            _logger.Log($"[LoadRecipeFromPlc] Connect {ip}:{settings.Port}");
            client.Connect();

            var ctrl = client.ReadHoldingRegisters((int)settings.ControlBaseAddr, 3);
            var state = ctrl[0];
            var rowCount = ctrl[2];

            if (state != StateIdle && state != StateWritingBlocked)
                throw new Exception($"Controller state not ready: {state}");

            if (rowCount <= 0) return new List<Step>();

            var intQty = rowCount * PinDataManager.CommunicationSettings.IntColumNum;
            var floatQty = rowCount * PinDataManager.CommunicationSettings.FloatColumNum * 2;
            var boolQty = rowCount * PinDataManager.CommunicationSettings.BoolColumNum / 16
                          + (rowCount * PinDataManager.CommunicationSettings.BoolColumNum % 16 > 0 ? 1 : 0);

            var intData = intQty > 0 ? ReadChunked(client, (int)settings.IntBaseAddr, intQty) : Array.Empty<int>();
            var floatData = floatQty > 0 ? ReadChunked(client, (int)settings.FloatBaseAddr, floatQty) : Array.Empty<int>();
            _ = boolQty; // not used in V1

            var steps = _mapper.FromRegisters(intData, floatData, rowCount);
            _logger.Log($"[LoadRecipeFromPlc] Rows: {steps.Count}");
            return steps;
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
            Debug.WriteLine($"[LoadRecipeFromPlc] Exception: {ex}");
            throw;
        }
        finally
        {
            try { client.Disconnect(); } catch { /* ignore */ }
        }
    }

    private static string BuildIp(PinDataManager.CommunicationSettings s)
        => $"{s.Ip1}.{s.Ip2}.{s.Ip3}.{s.Ip4}";

    /// <summary>
    /// Sends a write permission request to the PLC and waits for approval before proceeding.
    /// </summary>
    /// <param name="s">The communication settings containing control base addresses and configurations specific to the PLC.</param>
    /// <param name="c">The Modbus client used to communicate with the PLC for performing the write request.</param>
    /// <exception cref="Exception">Thrown when write permission cannot be obtained from the PLC after multiple attempts.</exception>
    private void RequestWritePermission(PinDataManager.CommunicationSettings s, ModbusClient c)
    {
        const int maxAttempts = 5;
        const int retryDelay = 50;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                c.WriteSingleRegister((int)(s.ControlBaseAddr + 1U), CmdWritingRequest);
                Thread.Sleep(retryDelay * attempt);

                var state = c.ReadHoldingRegisters((int)s.ControlBaseAddr, 1)[0];
                if (state == StateWritingAllowed)
                {
                    c.WriteSingleRegister((int)(s.ControlBaseAddr + 2U), 0);
                    return;
                }

                Thread.Sleep(retryDelay);
            }
            catch (Exception ex)
            {
                if (attempt == maxAttempts - 1) throw;
                _logger.Log($"[RequestWritePermission] Attempt {attempt + 1} error: {ex.Message}");
            }
        }

        throw new Exception("Write permission denied");
    }

    /// <summary>
    /// Completes the write operation to the PLC by updating the control registers.
    /// </summary>
    /// <param name="s">The communication settings containing base addresses and configuration data.</param>
    /// <param name="c">The Modbus client used to communicate with the PLC.</param>
    /// <param name="rows">The number of data rows written to the PLC.</param>
    private void CompleteWrite(PinDataManager.CommunicationSettings s, ModbusClient c, int rows)
    {
        c.WriteSingleRegister((int)(s.ControlBaseAddr + 2U), (ushort)rows);
        c.WriteSingleRegister((int)(s.ControlBaseAddr + 1U), CmdWritingNotActive);
    }

    /// <summary>
    /// Writes data to a Modbus server in chunks to handle large arrays efficiently.
    /// </summary>
    /// <param name="c">The Modbus client used to communicate with the server.</param>
    /// <param name="baseAddress">The starting address in the Modbus server where data will be written.</param>
    /// <param name="values">The array of integer values to be written to the Modbus server.</param>
    private void WriteChunked(ModbusClient c, int baseAddress, int[] values)
    {
        var total = values.Length;
        var index = 0;
        while (index < total)
        {
            var size = Math.Min(MaxChunkSize, total - index);
            var chunk = new int[size];
            Array.Copy(values, index, chunk, 0, size);
            c.WriteMultipleRegisters(baseAddress + index, chunk);
            index += size;
        }
    }

    /// <summary>
    /// Reads registers from a Modbus server in chunks, allowing handling of large data quantities.
    /// </summary>
    /// <param name="c">The Modbus client used to communicate with the server.</param>
    /// <param name="baseAddress">The starting address of the registers to be read.</param>
    /// <param name="totalRegisters">The total number of registers to read.</param>
    /// <returns>An array of integers containing the values read from the Modbus server.</returns>
    private int[] ReadChunked(ModbusClient c, int baseAddress, int totalRegisters)
    {
        var result = new int[totalRegisters];
        var index = 0;
        while (index < totalRegisters)
        {
            var size = Math.Min(MaxChunkSize, totalRegisters - index);
            var chunk = c.ReadHoldingRegisters(baseAddress + index, size);
            Array.Copy(chunk, 0, result, index, size);
            index += size;
        }
        return result;
    }
}