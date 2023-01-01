/*
    File name: GridServerArbiterBase.cs
    Written By: @networking-owk
    Description: Base class for all grid server arbiter implementations.

    Copyright MFDLABS 2001-2022. All rights reserved.
*/

namespace MFDLabs.Grid;

using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;

using ComputeCloud;

/// <summary>
/// Base class for all grid server arbiter implementations.
/// </summary>
public abstract class GridServerArbiterBase : IGridServerArbiter
{
    /// <inheritdoc cref="IGridServerArbiter.DiscoverInstances"/>
    public abstract IReadOnlyCollection<IGridServerInstance> DiscoverInstances();

    /// <inheritdoc cref="IGridServerArbiter.KillAllInstances"/>
    public abstract int KillAllInstances();

    /// <inheritdoc cref="IGridServerArbiter.KillInstanceByName(string, string)"/>
    public abstract bool KillInstanceByName(string name, string ipAddress = "127.0.0.1");

    /// <inheritdoc cref="IGridServerArbiter.RemoveInstance(IGridServerInstance)"/>
    public abstract void RemoveInstance(IGridServerInstance instance);

    /// <inheritdoc cref="IGridServerArbiter.CreateInstance(string, int, string, bool)"/>
    public abstract IGridServerInstance CreateInstance(string name = null, int maxAttemptsToCallSoap = 5, string ipAddress = "127.0.0.1", bool startUp = true);

    /// <inheritdoc cref="IGridServerArbiter.CreatePersistentInstance(string, int, string, bool, bool)"/>
    public abstract IGridServerInstance CreatePersistentInstance(
        string name,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool isPoolable = false,
        bool startUp = true
    );

    /// <inheritdoc cref="IGridServerArbiter.CreateLeasedInstance(string, TimeSpan?, int, string, bool)"/>
    public abstract ILeasedGridServerInstance CreateLeasedInstance(
        string name = null,
        TimeSpan? lease = null,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool startUp = true
    );

    /// <inheritdoc cref="IGridServerArbiter.BatchCreateInstances(int, int, string, bool)"/>
    public virtual IReadOnlyCollection<IGridServerInstance> BatchCreateInstances(
        int count = 1,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool startUp = true
    )
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count));
        if (!IPAddress.TryParse(ipAddress, out var _))
            throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        var instances = new List<IGridServerInstance>();
        for (var i = 0; i < count; i++)
            instances.Add(
                CreateInstance(
                    null,
                    maxAttemptsToCallSoap,
                    ipAddress,
                    startUp
                )
            );

        return instances;
    }

    /// <inheritdoc cref="IGridServerArbiter.BatchCreateLeasedInstances(TimeSpan?, int, int, string, bool)"/>
    public virtual IReadOnlyCollection<ILeasedGridServerInstance> BatchCreateLeasedInstances(
       TimeSpan? lease = null,
       int count = 1,
       int maxAttemptsToCallSoap = 5,
       string ipAddress = "127.0.0.1",
       bool startUp = true
    )
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count));
        if (!IPAddress.TryParse(ipAddress, out _))
            throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        var instances = new List<ILeasedGridServerInstance>();
        for (var i = 0; i < count; i++)
            instances.Add(
                CreateLeasedInstance(
                    null,
                    lease,
                    maxAttemptsToCallSoap,
                    ipAddress,
                    startUp
                )
            );

        return instances;
    }

    /// <inheritdoc cref="IGridServerArbiter.GetAllInstances"/>
    public abstract IReadOnlyCollection<IGridServerInstance> GetAllInstances();

    /// <inheritdoc cref="IGridServerArbiter.GetAllAvailableInstances"/>
    public abstract IReadOnlyCollection<IGridServerInstance> GetAllAvailableInstances();

    /// <inheritdoc cref="IGridServerArbiter.GetInstance"/>
    public abstract IGridServerInstance GetInstance(string name, string ipAddress = "127.0.0.1");

    /// <inheritdoc cref="IGridServerArbiter.GetPersistentInstance(string, string)"/>
    public abstract IGridServerInstance GetPersistentInstance(string name, string ipAddress = "127.0.0.1");

    /// <inheritdoc cref="IGridServerArbiter.GetLeasedInstance(string, string)"/>
    public abstract ILeasedGridServerInstance GetLeasedInstance(string name, string ipAddress = "127.0.0.1");

    /// <inheritdoc cref="IGridServerArbiter.GetAvailableInstance"/>
    public abstract IGridServerInstance GetAvailableInstance();

    /// <inheritdoc cref="IGridServerArbiter.GetAvailableLeasedInstance"/>
    public abstract ILeasedGridServerInstance GetAvailableLeasedInstance();

    /// <inheritdoc cref="IGridServerArbiter.GetOrCreateAnyInstance(string, int, string, bool)"/>
    public virtual IGridServerInstance GetOrCreateAnyInstance(
        string name = null,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool isPoolable = false
    )
    {
        if (!IPAddress.TryParse(ipAddress, out var _))
            throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        var instance = !string.IsNullOrEmpty(name) ?
            GetOrCreatePersistentInstance(name, maxAttemptsToCallSoap, ipAddress, isPoolable) :
            GetOrCreateAvailableInstance(maxAttemptsToCallSoap, ipAddress);

        return instance;
    }

    /// <inheritdoc cref="IGridServerArbiter.GetOrCreateInstance(string, int, string)"/>
    public virtual IGridServerInstance GetOrCreateInstance(string name, int maxAttemptsToCallSoap = 5, string ipAddress = "127.0.0.1")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        if (!IPAddress.TryParse(ipAddress, out var _))
            throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        var instance = GetInstance(name, ipAddress);
        return instance ?? CreateInstance(name, maxAttemptsToCallSoap, ipAddress);
    }

    /// <inheritdoc cref="IGridServerArbiter.GetOrCreatePersistentInstance(string, int, string, bool)"/>
    public virtual IGridServerInstance GetOrCreatePersistentInstance(
        string name,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool isPoolable = false
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        if (!IPAddress.TryParse(ipAddress, out var _))
            throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        var instance = GetPersistentInstance(name, ipAddress);
        return instance ?? CreatePersistentInstance(name, maxAttemptsToCallSoap, ipAddress, isPoolable);
    }

    /// <inheritdoc cref="IGridServerArbiter.GetOrCreateLeasedInstance(string, TimeSpan?, int, string)"/>
    public virtual ILeasedGridServerInstance GetOrCreateLeasedInstance(
        string name,
        TimeSpan? lease = null,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1"
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        if (!IPAddress.TryParse(ipAddress, out var _))
            throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        var instance = GetLeasedInstance(name, ipAddress);
        return instance ?? CreateLeasedInstance(name, lease, maxAttemptsToCallSoap, ipAddress);
    }

    /// <inheritdoc cref="IGridServerArbiter.GetOrCreateAvailableInstance(int, string)"/>
    public virtual IGridServerInstance GetOrCreateAvailableInstance(
       int maxAttemptsToCallSoap = 5,
       string ipAddress = "127.0.0.1"
    )
    {
        if (!IPAddress.TryParse(ipAddress, out var _))
            throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        var instance = GetAvailableInstance();
        return instance ?? CreateInstance(null, maxAttemptsToCallSoap, ipAddress);

    }

    /// <inheritdoc cref="IGridServerArbiter.GetOrCreateAvailableLeasedInstance(TimeSpan?, int, string)"/>
    public virtual ILeasedGridServerInstance GetOrCreateAvailableLeasedInstance(
        TimeSpan? lease = null,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1"
    )
    {
        if (!IPAddress.TryParse(ipAddress, out var _))
            throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        var instance = GetAvailableLeasedInstance();
        return instance ?? CreateLeasedInstance(null, lease, maxAttemptsToCallSoap, ipAddress);
    }

    /// <summary>
    /// Invoke a SOAP method on an instance that does not expect a return value.
    /// </summary>
    /// <param name="method">The SOAP method to invoke.</param>
    /// <param name="name">The name of the instance to invoke the method on. If null, a random instance will be selected.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP method.</param>
    /// <param name="ipAddress">The IP address of the instance to invoke the method on. If null, the default IP address will be used.</param>
    /// <param name="isPoolable">Should the instance be put on the pool of available instances?</param>
    /// <param name="args">The arguments to pass to the SOAP method.</param>
    protected void InvokeSoap(
        string method,
        string name = null,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool isPoolable = false,
        params object[] args
    ) => InvokeSoap<VoidResult>(method, name, maxAttemptsToCallSoap, ipAddress, isPoolable, args);

    /// <summary>
    /// Invoke a SOAP method on an instance.
    /// 
    /// For overrides, expect to handle <see cref="VoidResult"/>.
    /// </summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="method">The SOAP method to invoke.</param>
    /// <param name="name">The name of the instance to invoke the method on. If null, a random instance will be selected.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP method.</param>
    /// <param name="ipAddress">The IP address of the instance to invoke the method on. If null, the default IP address will be used.</param>
    /// <param name="isPoolable">Should the instance be put on the pool of available instances?</param>
    /// <param name="args">The arguments to pass to the SOAP method.</param>
    /// <returns>The return type.</returns>
    protected abstract T InvokeSoap<T>(
        string method,
        string name = null,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool isPoolable = false,
        params object[] args
    );

    /// <summary>
    /// Invoke a SOAP method on an instance that does not expect a return value asynchronously.
    /// </summary>
    /// <param name="method">The SOAP method to invoke.</param>
    /// <param name="name">The name of the instance to invoke the method on. If null, a random instance will be selected.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP method.</param>
    /// <param name="ipAddress">The IP address of the instance to invoke the method on. If null, the default IP address will be used.</param>
    /// <param name="isPoolable">Should the instance be put on the pool of available instances?</param>
    /// <param name="args">The arguments to pass to the SOAP method.</param>
    protected async Task InvokeSoapAsync(
        string method,
        string name = null,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool isPoolable = false,
        params object[] args
    ) => await InvokeSoapAsync<VoidResult>(method, name, maxAttemptsToCallSoap, ipAddress, isPoolable, args);

    /// <summary>
    /// Invoke a SOAP method on an instance asynchronously.
    /// 
    /// For overrides, expect to handle <see cref="VoidResult"/>.
    /// </summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="method">The SOAP method to invoke.</param>
    /// <param name="name">The name of the instance to invoke the method on. If null, a random instance will be selected.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP method.</param>
    /// <param name="ipAddress">The IP address of the instance to invoke the method on. If null, the default IP address will be used.</param>
    /// <param name="isPoolable">Should the instance be put on the pool of available instances?</param>
    /// <param name="args">The arguments to pass to the SOAP method.</param>
    /// <returns>The return type.</returns>
    protected abstract Task<T> InvokeSoapAsync<T>(
        string method,
        string name = null,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool isPoolable = false,
        params object[] args
    );

    #region |SOAP Methods|

    /// <inheritdoc cref="IGridServerArbiter.HelloWorld()"/>
    public string HelloWorld() => HelloWorld(5, "127.0.0.1");

    /// <inheritdoc cref="IGridServerArbiter.HelloWorld(int, string)"/>
    public string HelloWorld(int maxAttemptsToCallSoap, string ipAddress)
        => InvokeSoap<string>(nameof(HelloWorld), null, maxAttemptsToCallSoap, ipAddress, false);


    /// <inheritdoc cref="IGridServerArbiter.HelloWorldAsync()"/>
    public async Task<string> HelloWorldAsync() => await HelloWorldAsync(5, "127.0.0.1");

    /// <inheritdoc cref="IGridServerArbiter.HelloWorldAsync(int, string)"/>
    public async Task<string> HelloWorldAsync(int maxAttemptsToCallSoap, string ipAddress)
        => await InvokeSoapAsync<string>(nameof(HelloWorldAsync), null, maxAttemptsToCallSoap, ipAddress, false);


    /// <inheritdoc cref="IGridServerArbiter.GetVersion()"/>
    public string GetVersion() => GetVersion(5, "127.0.0.1");

    /// <inheritdoc cref="IGridServerArbiter.GetVersion(int, string)"/>
    public string GetVersion(int maxAttemptsToCallSoap, string ipAddress)
        => InvokeSoap<string>(nameof(GetVersion), null, maxAttemptsToCallSoap, ipAddress, false);


    /// <inheritdoc cref="IGridServerArbiter.GetVersionAsync()"/>
    public async Task<string> GetVersionAsync() => await GetVersionAsync(5, "127.0.0.1");

    /// <inheritdoc cref="IGridServerArbiter.GetVersionAsync(int, string)"/>
    public async Task<string> GetVersionAsync(int maxAttemptsToCallSoap, string ipAddress)
        => await InvokeSoapAsync<string>(nameof(GetVersionAsync), null, maxAttemptsToCallSoap, ipAddress, false);

    
    /// <inheritdoc cref="IGridServerArbiter.BatchJob(Job, ScriptExecution)"/>
    public LuaValue[] BatchJob(Job job, ScriptExecution script) => BatchJob(job, script, 5, "127.0.0.1");

    /// <inheritdoc cref="IGridServerArbiter.BatchJob(Job, ScriptExecution, int, string)"/>
    public LuaValue[] BatchJob(Job job, ScriptExecution script, int maxAttemptsToCallSoap, string ipAddress)
        => InvokeSoap<LuaValue[]>(nameof(BatchJob), null, maxAttemptsToCallSoap, ipAddress, false, job, script);


    /// <inheritdoc cref="IGridServerArbiter.BatchJobAsync(Job, ScriptExecution)"/>
    public async Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script) => await BatchJobAsync(job, script, 5, "127.0.0.1");

    /// <inheritdoc cref="IGridServerArbiter.BatchJobAsync(Job, ScriptExecution, int, string)"/>
    public async Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script, int maxAttemptsToCallSoap, string ipAddress)
        => await InvokeSoapAsync<BatchJobResponse>(nameof(BatchJobAsync), null, maxAttemptsToCallSoap, ipAddress, false, job, script);


    /// <inheritdoc cref="IGridServerArbiter.BatchJobEx(Job, ScriptExecution)"/>
    public LuaValue[] BatchJobEx(Job job, ScriptExecution script) => BatchJobEx(job, script, 5, "127.0.0.1");

    /// <inheritdoc cref="IGridServerArbiter.BatchJobEx(Job, ScriptExecution, int, string)"/>
    public LuaValue[] BatchJobEx(Job job, ScriptExecution script, int maxAttemptsToCallSoap, string ipAddress)
        => InvokeSoap<LuaValue[]>(nameof(BatchJobEx), null, maxAttemptsToCallSoap, ipAddress, false, job, script);


    /// <inheritdoc cref="IGridServerArbiter.BatchJobExAsync(Job, ScriptExecution)"/>
    public async Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script) => await BatchJobExAsync(job, script, 5, "127.0.0.1");

    /// <inheritdoc cref="IGridServerArbiter.BatchJobExAsync(Job, ScriptExecution, int, string)"/>
    public async Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script, int maxAttemptsToCallSoap, string ipAddress)
        => await InvokeSoapAsync<LuaValue[]>(nameof(BatchJobExAsync), null, maxAttemptsToCallSoap, ipAddress, false, job, script);

    #endregion |SOAP Methods|
}
