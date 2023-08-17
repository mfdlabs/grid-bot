namespace Redis;

using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using StackExchange.Redis;

public partial class SelfHealingConnectionBuilder
{
    private partial class SelfHealingConnectionMultiplexer
    {
        private class BadStateReportingServer : BadStateReportingBase<IServer>, IServer, IRedis, IRedisAsync
        {
            public override event Action BadStateExceptionOccurred;

            public ClusterConfiguration ClusterConfiguration => Decorated.ClusterConfiguration;
            public EndPoint EndPoint => Decorated.EndPoint;
            public RedisFeatures Features => Decorated.Features;
            public bool IsConnected => Decorated.IsConnected;
            public bool IsSlave => Decorated.IsSlave;
            public bool AllowSlaveWrites
            {
                get => Decorated.AllowSlaveWrites;
                set => Decorated.AllowSlaveWrites = value;
            }
            public ServerType ServerType => Decorated.ServerType;
            public Version Version => Decorated.Version;
            public ConnectionMultiplexer Multiplexer => Decorated.Multiplexer;

            public BadStateReportingServer(IServer decorated)
                : base(decorated)
            {
            }

            protected override void RaiseBadStateExceptionOccurred()
                => BadStateExceptionOccurred?.Invoke();


            public void ClientKill(EndPoint endpoint, CommandFlags flags = CommandFlags.None)
            => DoDecoratedOperation(s => s.ClientKill(endpoint, flags));


            public Task ClientKillAsync(EndPoint endpoint, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ClientKillAsync(endpoint, flags));


            public long ClientKill(long? id = null, ClientType? clientType = null, EndPoint endpoint = null, bool skipMe = true, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ClientKill(id, clientType, endpoint, skipMe, flags));


            public Task<long> ClientKillAsync(long? id = null, ClientType? clientType = null, EndPoint endpoint = null, bool skipMe = true, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ClientKillAsync(id, clientType, endpoint, skipMe, flags));


            public ClientInfo[] ClientList(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ClientList(flags));


            public Task<ClientInfo[]> ClientListAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ClientListAsync(flags));


            public ClusterConfiguration ClusterNodes(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ClusterNodes(flags));


            public Task<ClusterConfiguration> ClusterNodesAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ClusterNodesAsync(flags));


            public string ClusterNodesRaw(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ClusterNodesRaw(flags));


            public Task<string> ClusterNodesRawAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ClusterNodesRawAsync(flags));


            public KeyValuePair<string, string>[] ConfigGet(RedisValue pattern = default(RedisValue), CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ConfigGet(pattern, flags));


            public Task<KeyValuePair<string, string>[]> ConfigGetAsync(RedisValue pattern = default(RedisValue), CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ConfigGetAsync(pattern, flags));


            public void ConfigResetStatistics(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ConfigResetStatistics(flags));

            public Task ConfigResetStatisticsAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ConfigResetStatisticsAsync(flags));


            public void ConfigRewrite(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ConfigRewrite(flags));

            public Task ConfigRewriteAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ConfigRewriteAsync(flags));


            public void ConfigSet(RedisValue setting, RedisValue value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ConfigSet(setting, value, flags));


            public Task ConfigSetAsync(RedisValue setting, RedisValue value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ConfigSetAsync(setting, value, flags));


            public long DatabaseSize(int database = 0, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.DatabaseSize(database, flags));


            public Task<long> DatabaseSizeAsync(int database = 0, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.DatabaseSizeAsync(database, flags));


            public RedisValue Echo(RedisValue message, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.Echo(message, flags));


            public Task<RedisValue> EchoAsync(RedisValue message, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.EchoAsync(message, flags));


            public void FlushAllDatabases(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.FlushAllDatabases(flags));


            public Task FlushAllDatabasesAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.FlushAllDatabasesAsync(flags));


            public void FlushDatabase(int database = 0, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.FlushDatabase(database, flags));

            public Task FlushDatabaseAsync(int database = 0, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.FlushDatabaseAsync(database, flags));


            public ServerCounters GetCounters()
                => DoDecoratedOperation(s => s.GetCounters());


            public IGrouping<string, KeyValuePair<string, string>>[] Info(RedisValue section = default(RedisValue), CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.Info(section, flags));


            public Task<IGrouping<string, KeyValuePair<string, string>>[]> InfoAsync(RedisValue section = default(RedisValue), CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.InfoAsync(section, flags));


            public string InfoRaw(RedisValue section = default(RedisValue), CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.InfoRaw(section, flags));


            public Task<string> InfoRawAsync(RedisValue section = default(RedisValue), CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.InfoRawAsync(section, flags));


            public IEnumerable<RedisKey> Keys(int database, RedisValue pattern, int pageSize, CommandFlags flags)
                => DoDecoratedOperation(s => s.Keys(database, pattern, pageSize, flags));


            public IEnumerable<RedisKey> Keys(int database = 0, RedisValue pattern = default(RedisValue), int pageSize = 10, long cursor = 0L, int pageOffset = 0, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.Keys(database, pattern, pageSize, cursor, pageOffset, flags));


            public DateTime LastSave(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.LastSave(flags));


            public Task<DateTime> LastSaveAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.LastSaveAsync(flags));


            public void MakeMaster(ReplicationChangeOptions options, TextWriter log = null)
                => DoDecoratedOperation(s => s.MakeMaster(options, log));

            public void Save(SaveType type, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.Save(type, flags));


            public Task SaveAsync(SaveType type, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SaveAsync(type, flags));


            public bool ScriptExists(string script, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ScriptExists(script, flags));


            public bool ScriptExists(byte[] sha1, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ScriptExists(sha1, flags));


            public Task<bool> ScriptExistsAsync(string script, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ScriptExistsAsync(script, flags));


            public Task<bool> ScriptExistsAsync(byte[] sha1, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ScriptExistsAsync(sha1, flags));


            public void ScriptFlush(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ScriptFlush(flags));


            public Task ScriptFlushAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ScriptFlushAsync(flags));


            public byte[] ScriptLoad(string script, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ScriptLoad(script, flags));


            public LoadedLuaScript ScriptLoad(LuaScript script, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ScriptLoad(script, flags));


            public Task<byte[]> ScriptLoadAsync(string script, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ScriptLoadAsync(script, flags));


            public Task<LoadedLuaScript> ScriptLoadAsync(LuaScript script, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.ScriptLoadAsync(script, flags));


            public void Shutdown(ShutdownMode shutdownMode = ShutdownMode.Default, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.Shutdown(shutdownMode, flags));

            public void SlaveOf(EndPoint master, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SlaveOf(master, flags));


            public Task SlaveOfAsync(EndPoint master, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SlaveOfAsync(master, flags));


            public CommandTrace[] SlowlogGet(int count = 0, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SlowlogGet(count, flags));


            public Task<CommandTrace[]> SlowlogGetAsync(int count = 0, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SlowlogGetAsync(count, flags));


            public void SlowlogReset(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SlowlogReset(flags));


            public Task SlowlogResetAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SlowlogResetAsync(flags));


            public RedisChannel[] SubscriptionChannels(RedisChannel pattern = default(RedisChannel), CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SubscriptionChannels(pattern, flags));


            public Task<RedisChannel[]> SubscriptionChannelsAsync(RedisChannel pattern = default(RedisChannel), CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SubscriptionChannelsAsync(pattern, flags));


            public long SubscriptionPatternCount(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SubscriptionPatternCount(flags));


            public Task<long> SubscriptionPatternCountAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SubscriptionPatternCountAsync(flags));


            public long SubscriptionSubscriberCount(RedisChannel channel, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SubscriptionSubscriberCount(channel, flags));


            public Task<long> SubscriptionSubscriberCountAsync(RedisChannel channel, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SubscriptionSubscriberCountAsync(channel, flags));


            public DateTime Time(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.Time(flags));


            public Task<DateTime> TimeAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.TimeAsync(flags));


            public EndPoint SentinelGetMasterAddressByName(string serviceName, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SentinelGetMasterAddressByName(serviceName, flags));


            public Task<EndPoint> SentinelGetMasterAddressByNameAsync(string serviceName, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SentinelGetMasterAddressByNameAsync(serviceName, flags));


            public KeyValuePair<string, string>[] SentinelMaster(string serviceName, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SentinelMaster(serviceName, flags));


            public Task<KeyValuePair<string, string>[]> SentinelMasterAsync(string serviceName, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SentinelMasterAsync(serviceName, flags));


            public KeyValuePair<string, string>[][] SentinelMasters(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SentinelMasters(flags));


            public Task<KeyValuePair<string, string>[][]> SentinelMastersAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SentinelMastersAsync(flags));


            public KeyValuePair<string, string>[][] SentinelSlaves(string serviceName, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SentinelSlaves(serviceName, flags));


            public Task<KeyValuePair<string, string>[][]> SentinelSlavesAsync(string serviceName, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SentinelSlavesAsync(serviceName, flags));


            public void SentinelFailover(string serviceName, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SentinelFailover(serviceName, flags));


            public Task SentinelFailoverAsync(string serviceName, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SentinelFailoverAsync(serviceName, flags));


            public TimeSpan Ping(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.Ping(flags));


            public Task<TimeSpan> PingAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.PingAsync(flags));


            public bool TryWait(Task task)
                => DoDecoratedOperation(t => t.TryWait(task));


            public void Wait(Task task)
                => DoDecoratedOperation(t => t.Wait(task));


            public T Wait<T>(Task<T> task)
                => DoDecoratedOperation(t => t.Wait(task));


            public void WaitAll(params Task[] tasks)
                => DoDecoratedOperation(t => t.WaitAll(tasks));
        }
    }
}
