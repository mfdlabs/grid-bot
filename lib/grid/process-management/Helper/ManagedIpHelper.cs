namespace Grid;

using System;
using System.Net;
using System.Collections;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

using PInvoke;

#region Managed IP Helper API

internal class TcpTable : IEnumerable<TcpRow>
{
    #region Private Fields

    private readonly IEnumerable<TcpRow> _tcpRows;

    #endregion

    #region Constructors

    public TcpTable(IEnumerable<TcpRow> tcpRows)
    {
        _tcpRows = tcpRows;
    }

    #endregion

    #region Public Properties

    public IEnumerable<TcpRow> Rows => _tcpRows;

    #endregion

    #region IEnumerable<TcpRow> Members

    public IEnumerator<TcpRow> GetEnumerator() => _tcpRows.GetEnumerator();

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator() => _tcpRows.GetEnumerator();

    #endregion
}

internal class TcpRow
{
    #region Private Fields

    private readonly IPEndPoint _localEndPoint;
    private readonly IPEndPoint _remoteEndPoint;
    private readonly TcpState _state;
    private readonly uint _processId;

    #endregion

    #region Constructors

    public TcpRow(IPHlpApi.MIB_TCPROW_OWNER_PID tcpRow)
    {
        _state = tcpRow.dwState;
        _processId = tcpRow.dwOwningPid;
        _localEndPoint = new IPEndPoint(tcpRow.LocalAddr, tcpRow.LocalPort);
        _remoteEndPoint = new IPEndPoint(tcpRow.RemoteAddr, tcpRow.RemotePort);
    }

    #endregion

    #region Public Properties

    public IPEndPoint LocalEndPoint => _localEndPoint;

    public IPEndPoint RemoteEndPoint => _remoteEndPoint;

    public TcpState State => _state;

    public uint ProcessId => _processId;

    #endregion
}

/// <summary>
/// This is repurposed and enhanced code from 2007
/// </summary>
internal static class ManagedIpHelper
{
    #region Public Methods

    public static TcpTable GetExtendedTcpTable(bool sorted)
    {
        var tcpRows = new List<TcpRow>();
        
        var tcpTable = IntPtr.Zero;
        int tcpTableLength = 0;

        if (
            IPHlpApi.GetExtendedTcpTable(
                tcpTable, 
                ref tcpTableLength, 
                sorted, 
                AddressFamily.InterNetwork, 
                IPHlpApi.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 
                0
            ) != Win32ErrorCode.ERROR_SUCCESS
        )
        {
            try
            {
                tcpTable = Marshal.AllocHGlobal(tcpTableLength);
                if (
                    IPHlpApi.GetExtendedTcpTable(
                        tcpTable, 
                        ref tcpTableLength, 
                        true, 
                        AddressFamily.InterNetwork, 
                        IPHlpApi.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 
                        0
                    ) == Win32ErrorCode.ERROR_SUCCESS
                )
                {
                    var table = (IPHlpApi.MIB_TCPTABLE_OWNER_PID)Marshal.PtrToStructure(tcpTable, typeof(IPHlpApi.MIB_TCPTABLE_OWNER_PID));

                    var rowPtr = (IntPtr)((long)tcpTable + Marshal.SizeOf(table.dwNumEntries));
                    for (int i = 0; i < table.dwNumEntries; ++i)
                    {
                        tcpRows.Add(new TcpRow((IPHlpApi.MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(IPHlpApi.MIB_TCPROW_OWNER_PID))));
                        rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(typeof(IPHlpApi.MIB_TCPROW_OWNER_PID)));
                    }
                }
            }
            finally
            {
                if (tcpTable != IntPtr.Zero)
                    Marshal.FreeHGlobal(tcpTable);
            }
        }

        return new TcpTable(tcpRows);
    }

    #endregion
}

#endregion
