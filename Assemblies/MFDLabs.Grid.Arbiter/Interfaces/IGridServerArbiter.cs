/*
    File name: IGridServerArbiter.cs
    Written By: @networking-owk
    Description: The base interface for the Grid Server Arbiter

    Copyright MFDLABS 2001-2022. All rights reserved.
*/

namespace MFDLabs.Grid;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using ComputeCloud;

/// <summary>
/// The base interface for the Grid Server Arbiter.
/// 
/// Only GetVersion and BatchJob methods are exposed on the arbiter as they are short lived and do not require any information about jobs.
/// </summary>
public interface IGridServerArbiter
{
    /// <summary>
    /// Discover the grid servers available on the network.
    /// </summary>
    /// <remarks>
    /// As we have no arbiter information on these instances, 
    /// the default implementation for this inside of <see cref="GridServerArbiter"/> is to return a list of <see cref="LeasedGridServerInstance"/> instances.
    /// </remarks>
    /// <returns>A list of grid servers available on the network.</returns>
    IReadOnlyCollection<IGridServerInstance> DiscoverInstances();

    /// <summary>
    /// Kills all instances managed by the arbiter
    /// </summary>
    /// <returns>The number of instances killed</returns>
    int KillAllInstances();

    /// <summary>
    /// Kills an instance managed by the arbiter
    /// </summary>
    /// <param name="name">The name of the instance to kill</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <returns>True if the instance was killed successfully or not.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> cannot be null.</exception>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    bool KillInstanceByName(string name, string ipAddress = "127.0.0.1");

    /// <summary>
    /// Remove an instance from the arbiter.
    /// </summary>
    /// <param name="instance">The instance to remove.</param>
    void RemoveInstance(IGridServerInstance instance);

    /// <summary>
    /// Create a single use, arbitered instance.
    /// </summary>
    /// <param name="name">The name of the instance. If null, a random name will be generated.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <param name="startUp">Should the instance's process be started up?</param>
    /// <returns>The instance that was created.</returns>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    IGridServerInstance CreateInstance(string name = null, int maxAttemptsToCallSoap = 5, string ipAddress = "127.0.0.1", bool startUp = true);

    /// <summary>
    /// Create a multi-use, user managed persistent instance.
    /// </summary>
    /// <param name="name">The name of the instance.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <param name="isPoolable">Is the instance allowed to be executed on?</param>
    /// <param name="startUp">Should the instance's process be started up?</param>
    /// <returns>The instance that was created.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> cannot be null.</exception>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    IGridServerInstance CreatePersistentInstance(
        string name,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool isPoolable = false,
        bool startUp = true
    );

    /// <summary>
    /// Create a multi-use, user managed instance that expires after a certain amount of time.
    /// </summary>
    /// <param name="name">The name of the instance. If null, a random name will be generated.</param>
    /// <param name="lease">The lease time of the instance. If null, <see cref="LeasedGridServerInstance.DefaultLease"/> will be used.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <param name="startUp">Should the instance's process be started up?</param>
    /// <returns>The instance that was created.</returns>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    ILeasedGridServerInstance CreateLeasedInstance(
        string name = null,
        TimeSpan? lease = null,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool startUp = true
    );

    /// <summary>
    /// Create multiple single use, arbitered instances.
    /// </summary>
    /// <param name="count">The number of instances to create. Default is 1.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <param name="startUp">Should the instance's process be started up?</param>
    /// <returns>The instances that were created.</returns>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    IReadOnlyCollection<IGridServerInstance> BatchCreateInstances(
        int count = 1, 
        int maxAttemptsToCallSoap = 5, 
        string ipAddress = "127.0.0.1", 
        bool startUp = true
    );

    /// <summary>
    /// Create multiple multi-use, user managed instances that expire after a certain amount of time.
    /// </summary>
    /// <param name="lease">The lease time of the instance. If null, <see cref="LeasedGridServerInstance.DefaultLease"/> will be used.</param>
    /// <param name="count">The number of instances to create. Default is 1.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <param name="startUp">Should the instance's process be started up?</param>
    /// <returns>The instances that were created.</returns>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    IReadOnlyCollection<ILeasedGridServerInstance> BatchCreateLeasedInstances(
        TimeSpan? lease = null,
        int count = 1,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool startUp = true
    );

    /// <summary>
    /// Get all instances owned by this arbiter.
    /// </summary>
    /// <returns>The instances owned by this arbiter.</returns>
    IReadOnlyCollection<IGridServerInstance> GetAllInstances();

    /// <summary>
    /// Get all available instances owned by this arbiter.
    /// </summary>
    /// <returns>The instances owned by this arbiter that are available.</returns>
    IReadOnlyCollection<IGridServerInstance> GetAllAvailableInstances();

    /// <summary>
    /// Get an instance by name.
    /// </summary>
    /// <param name="name">The name of the instance.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <returns>The instance with the specified name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> cannot be null.</exception>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    IGridServerInstance GetInstance(string name, string ipAddress = "127.0.0.1");

    /// <summary>
    /// Get a persistent instance by name.
    /// </summary>
    /// <param name="name">The name of the instance.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <returns>The instance with the specified name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> cannot be null.</exception>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    IGridServerInstance GetPersistentInstance(string name, string ipAddress = "127.0.0.1");

    /// <summary>
    /// Get a leased instance by name.
    /// </summary>
    /// <param name="name">The name of the instance.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <returns>The instance with the specified name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> cannot be null.</exception>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    ILeasedGridServerInstance GetLeasedInstance(string name, string ipAddress = "127.0.0.1");

    /// <summary>
    /// Get an available instance.
    /// </summary>
    /// <returns>Any instances that are available.</returns>
    IGridServerInstance GetAvailableInstance();

    /// <summary>
    /// Get an available leased instance.
    /// </summary>
    /// <returns>Any leased instances that are available.</returns>
    ILeasedGridServerInstance GetAvailableLeasedInstance();

    /// <summary>
    /// Get or create an instance. 
    /// 
    /// If <paramref name="name"/> is not specified, it gets an available instance.
    /// </summary>
    /// <param name="name">The name of the instance.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <param name="isPoolable">Is the instance allowed to be executed on?</param>
    /// <returns>The instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    IGridServerInstance GetOrCreateAnyInstance(
        string name = null,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool isPoolable = false
    );

    /// <summary>
    /// Get or create an instance by name.
    /// </summary>
    /// <param name="name">The name of the instance.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <returns>The instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> cannot be null.</exception>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    IGridServerInstance GetOrCreateInstance(string name, int maxAttemptsToCallSoap = 5, string ipAddress = "127.0.0.1");

    /// <summary>
    /// Get or create an instance by name.
    /// </summary>
    /// <param name="name">The name of the instance.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <param name="isPoolable">Is the instance allowed to be executed on?</param>
    /// <returns>The instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> cannot be null.</exception>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    IGridServerInstance GetOrCreatePersistentInstance(
        string name,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool isPoolable = false
    );

    /// <summary>
    /// Get or create an instance by name.
    /// </summary>
    /// <param name="name">The name of the instance.</param>
    /// <param name="lease">The lease time of the instance. If null, <see cref="LeasedGridServerInstance.DefaultLease"/> will be used.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <returns>The instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> cannot be null.</exception>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    ILeasedGridServerInstance GetOrCreateLeasedInstance(string name, TimeSpan? lease = null, int maxAttemptsToCallSoap = 5, string ipAddress = "127.0.0.1");

    /// <summary>
    /// Get or create any available instance.
    /// </summary>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <returns>The instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    IGridServerInstance GetOrCreateAvailableInstance(int maxAttemptsToCallSoap = 5, string ipAddress = "127.0.0.1");

    /// <summary>
    /// Get or create any available leased instance.
    /// </summary>
    /// <param name="lease">The lease time of the instance. If null, <see cref="LeasedGridServerInstance.DefaultLease"/> will be used.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <returns>The instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="ipAddress"/> is not a valid IP address.</exception>
    ILeasedGridServerInstance GetOrCreateAvailableLeasedInstance(TimeSpan? lease = null, int maxAttemptsToCallSoap = 5, string ipAddress = "127.0.0.1");


    /// <summary>
    /// Make a hello world call on a random instance.
    /// </summary>
    /// <returns>The hello world string.</returns>
    string HelloWorld();

    /// <summary>
    /// Make a hello world call on a random instance.
    /// </summary>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <returns>The hello world string.</returns>
    string HelloWorld(int maxAttemptsToCallSoap, string ipAddress);


    /// <summary>
    /// Make a hello world call on a random instance asynchronously.
    /// </summary>
    /// <returns>The hello world string.</returns>
    Task<string> HelloWorldAsync();

    /// <summary>
    /// Make a hello world call on a random instance asynchronously.
    /// </summary>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <returns>The hello world string.</returns>
    Task<string> HelloWorldAsync(int maxAttemptsToCallSoap, string ipAddress);


    /// <summary>
    /// Get the grid server version.
    /// </summary>
    /// <returns>The version.</returns>
    string GetVersion();

    /// <summary>
    /// Get the grid server version.
    /// </summary>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <returns>The version.</returns>
    string GetVersion(int maxAttemptsToCallSoap, string ipAddress);
    

    /// <summary>
    /// Get the grid server version asynchronously.
    /// </summary>
    /// <returns>The version.</returns>
    Task<string> GetVersionAsync();

    /// <summary>
    /// Get the grid server version asynchronously.
    /// </summary>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <returns>The version.</returns>
    Task<string> GetVersionAsync(int maxAttemptsToCallSoap, string ipAddress);


    /// <summary>
    /// Make a batch job call on a random instance.
    /// </summary>
    /// <param name="job">The information on the <see cref="Job"/>.</param>
    /// <param name="script">The script to execute.</param>
    /// <returns>The result of the script.</returns>
    [Obsolete($"{nameof(BatchJob)} is deprecated, use {nameof(BatchJobEx)} instead.")]
    LuaValue[] BatchJob(Job job, ScriptExecution script);

    /// <summary>
    /// Make a batch job call on a random instance.
    /// </summary>
    /// <param name="job">The information on the <see cref="Job"/>.</param>
    /// <param name="script">The script to execute.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <returns>The result of the script.</returns>
    [Obsolete($"{nameof(BatchJob)} is deprecated, use {nameof(BatchJobEx)} instead.")]
    LuaValue[] BatchJob(Job job, ScriptExecution script, int maxAttemptsToCallSoap, string ipAddress);
    

    /// <summary>
    /// Make a batch job call on a random instance asynchronously.
    /// </summary>
    /// <param name="job">The information on the <see cref="Job"/>.</param>
    /// <param name="script">The script to execute.</param>
    Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script);

    /// <summary>
    /// Make a batch job call on a random instance asynchronously.
    /// </summary>
    /// <param name="job">The information on the <see cref="Job"/>.</param>
    /// <param name="script">The script to execute.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    [Obsolete($"{nameof(BatchJobAsync)} is deprecated, use {nameof(BatchJobExAsync)} instead.")]
    Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script, int maxAttemptsToCallSoap, string ipAddress);
    

    /// <summary>
    /// Make a batch job call on a random instance.
    /// </summary>
    /// <param name="job">The information on the <see cref="Job"/>.</param>
    /// <param name="script">The script to execute.</param>
    /// <returns>The result of the script.</returns>
    LuaValue[] BatchJobEx(Job job, ScriptExecution script);

    /// <summary>
    /// Make a batch job call on a random instance.
    /// </summary>
    /// <param name="job">The information on the <see cref="Job"/>.</param>
    /// <param name="script">The script to execute.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    /// <returns>The result of the script.</returns>
    LuaValue[] BatchJobEx(Job job, ScriptExecution script, int maxAttemptsToCallSoap, string ipAddress);
    

    /// <summary>
    /// Make a batch job call on a random instance asynchronously.
    /// </summary>
    /// <param name="job">The information on the <see cref="Job"/>.</param>
    /// <param name="script">The script to execute.</param>
    Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script);

    /// <summary>
    /// Make a batch job call on a random instance asynchronously.
    /// </summary>
    /// <param name="job">The information on the <see cref="Job"/>.</param>
    /// <param name="script">The script to execute.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum number of attempts to call the SOAP service.</param>
    /// <param name="ipAddress">The IP address of the instance.</param>
    Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script, int maxAttemptsToCallSoap, string ipAddress);
}
