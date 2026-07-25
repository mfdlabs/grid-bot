namespace Grid.ProcessManagement;

using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Networking.WinSock;
using Windows.Win32.NetworkManagement.IpHelper;

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
    private readonly MIB_TCP_STATE _state;
    private readonly uint _processId;

    #endregion

    #region Constructors

    public TcpRow(MIB_TCPROW_OWNER_PID tcpRow)
    {
        _state = tcpRow.dwState;
        _processId = tcpRow.dwOwningPid;

        var localPort = PInvoke.ntohs((ushort)tcpRow.dwLocalPort);
        var remotePort = PInvoke.ntohs((ushort)tcpRow.dwRemotePort);

        if (tcpRow.dwLocalPort <= 0)
            _localEndPoint = new IPEndPoint(tcpRow.dwLocalAddr, 0);
        else
            _localEndPoint = new IPEndPoint(tcpRow.dwLocalAddr, localPort);

        if (tcpRow.dwRemotePort <= 0)
            _remoteEndPoint = new IPEndPoint(tcpRow.dwRemoteAddr, 0);
        else
            _remoteEndPoint = new IPEndPoint(tcpRow.dwRemoteAddr, remotePort);
    }

    #endregion

    #region Public Properties

    public IPEndPoint LocalEndPoint => _localEndPoint;

    public IPEndPoint RemoteEndPoint => _remoteEndPoint;

    public MIB_TCP_STATE State => _state;

    public uint ProcessId => _processId;

    #endregion
}

/// <summary>
/// This is repurposed and enhanced code from 2007
/// </summary>
internal static class ManagedIpHelper
{
    #region Public Methods

    public unsafe static TcpTable GetExtendedTcpTable(bool sorted)
    {
        var tcpRows = new List<TcpRow>();

        void* pTcpTable = null;
        uint pdwSize = 0;

        try
        {

            if (
                PInvoke.GetExtendedTcpTable(
                    null,
                    ref pdwSize,
                    sorted,
                    (uint)ADDRESS_FAMILY.AF_INET,
                    TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_LISTENER,
                    0
                ) != (uint)WIN32_ERROR.NO_ERROR
            )
            {
                pTcpTable = (void*)Marshal.AllocHGlobal((int)pdwSize);

                if (
                    PInvoke.GetExtendedTcpTable(
                        pTcpTable,
                        ref pdwSize,
                        sorted,
                        (uint)ADDRESS_FAMILY.AF_INET,
                        TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_LISTENER,
                        0
                    ) == (uint)WIN32_ERROR.NO_ERROR
                )
                {
                    var table = (MIB_TCPTABLE_OWNER_PID*)pTcpTable;

                    for (int i = 0; i < table->dwNumEntries; ++i)
                        tcpRows.Add(new TcpRow(table->table[i]));
                }
            }
        }
        finally
        {
            if (pTcpTable != null)
                Marshal.FreeHGlobal((IntPtr)pTcpTable);
        }

        return new TcpTable(tcpRows);
    }

    #endregion
}

#endregion
