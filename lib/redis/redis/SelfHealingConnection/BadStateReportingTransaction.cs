namespace Redis;

using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;

using StackExchange.Redis;

public partial class SelfHealingConnectionBuilder
{
    private partial class SelfHealingConnectionMultiplexer
    {
        private class BadStateReportingTransaction : BadStateReportingBase<ITransaction>, ITransaction, IDatabaseAsync, IRedisAsync
        {
            public override event Action BadStateExceptionOccurred;

            public ConnectionMultiplexer Multiplexer => Decorated.Multiplexer;

            public BadStateReportingTransaction(ITransaction decorated)
                : base(decorated)
            {
            }

            protected override void RaiseBadStateExceptionOccurred()
                => BadStateExceptionOccurred?.Invoke();


            public ConditionResult AddCondition(Condition condition)
                => Decorated.AddCondition(condition);

            public bool Execute(CommandFlags flags = CommandFlags.None)
                => base.Decorated.Execute(flags);

            public Task<bool> ExecuteAsync(CommandFlags flags = CommandFlags.None)
                => Decorated.ExecuteAsync(flags);

            public void Execute() => Decorated.Execute(CommandFlags.None);
            public Task<RedisValue> DebugObjectAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.DebugObjectAsync(key, flags));

            public Task<bool> GeoAddAsync(RedisKey key, double longitude, double latitude, RedisValue member, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.GeoAddAsync(key, longitude, latitude, member, flags));

            public Task<bool> GeoAddAsync(RedisKey key, GeoEntry value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.GeoAddAsync(key, value, flags));

            public Task<long> GeoAddAsync(RedisKey key, GeoEntry[] values, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.GeoAddAsync(key, values, flags));

            public Task<double?> GeoDistanceAsync(RedisKey key, RedisValue member1, RedisValue member2, GeoUnit unit = GeoUnit.Meters, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.GeoDistanceAsync(key, member1, member2, unit, flags));

            public Task<string[]> GeoHashAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.GeoHashAsync(key, members, flags));


            public Task<string> GeoHashAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.GeoHashAsync(key, member, flags));


            public Task<GeoPosition?[]> GeoPositionAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.GeoPositionAsync(key, members, flags));


            public Task<GeoPosition?> GeoPositionAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.GeoPositionAsync(key, member, flags));


            public Task<GeoRadiusResult[]> GeoRadiusAsync(RedisKey key, RedisValue member, double radius, GeoUnit unit = GeoUnit.Meters, int count = -1, Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.GeoRadiusAsync(key, member, radius, unit, count, order, options, flags));


            public Task<GeoRadiusResult[]> GeoRadiusAsync(RedisKey key, double longitude, double latitude, double radius, GeoUnit unit = GeoUnit.Meters, int count = -1, Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.GeoRadiusAsync(key, longitude, latitude, radius, unit, count, order, options, flags));


            public Task<bool> GeoRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.GeoRemoveAsync(key, member, flags));


            public Task<long> HashDecrementAsync(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashDecrementAsync(key, hashField, value, flags));


            public Task<double> HashDecrementAsync(RedisKey key, RedisValue hashField, double value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashDecrementAsync(key, hashField, value, flags));


            public Task<bool> HashDeleteAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashDeleteAsync(key, hashField, flags));


            public Task<long> HashDeleteAsync(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashDeleteAsync(key, hashFields, flags));


            public Task<bool> HashExistsAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashExistsAsync(key, hashField, flags));


            public Task<HashEntry[]> HashGetAllAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashGetAllAsync(key, flags));


            public Task<RedisValue> HashGetAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashGetAsync(key, hashField, flags));


            public Task<RedisValue[]> HashGetAsync(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashGetAsync(key, hashFields, flags));


            public Task<long> HashIncrementAsync(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashIncrementAsync(key, hashField, value, flags));


            public Task<double> HashIncrementAsync(RedisKey key, RedisValue hashField, double value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashIncrementAsync(key, hashField, value, flags));


            public Task<RedisValue[]> HashKeysAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashKeysAsync(key, flags));


            public Task<long> HashLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashLengthAsync(key, flags));


            public Task HashSetAsync(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashSetAsync(key, hashFields, flags));


            public Task<bool> HashSetAsync(RedisKey key, RedisValue hashField, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashSetAsync(key, hashField, value, when, flags));


            public Task<RedisValue[]> HashValuesAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HashValuesAsync(key, flags));


            public Task<bool> HyperLogLogAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HyperLogLogAddAsync(key, value, flags));


            public Task<bool> HyperLogLogAddAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HyperLogLogAddAsync(key, values, flags));


            public Task<long> HyperLogLogLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HyperLogLogLengthAsync(key, flags));


            public Task<long> HyperLogLogLengthAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HyperLogLogLengthAsync(keys, flags));


            public Task HyperLogLogMergeAsync(RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HyperLogLogMergeAsync(destination, first, second, flags));


            public Task HyperLogLogMergeAsync(RedisKey destination, RedisKey[] sourceKeys, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.HyperLogLogMergeAsync(destination, sourceKeys, flags));


            public Task<EndPoint> IdentifyEndpointAsync(RedisKey key = default(RedisKey), CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.IdentifyEndpointAsync(key, flags));


            public bool IsConnected(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.IsConnected(key, flags));


            public Task<bool> KeyDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.KeyDeleteAsync(key, flags));


            public Task<long> KeyDeleteAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.KeyDeleteAsync(keys, flags));


            public Task<byte[]> KeyDumpAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.KeyDumpAsync(key, flags));


            public Task<bool> KeyExistsAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.KeyExistsAsync(key, flags));


            public Task<bool> KeyExpireAsync(RedisKey key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.KeyExpireAsync(key, expiry, flags));


            public Task<bool> KeyExpireAsync(RedisKey key, DateTime? expiry, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.KeyExpireAsync(key, expiry, flags));


            public Task KeyMigrateAsync(RedisKey key, EndPoint toServer, int toDatabase = 0, int timeoutMilliseconds = 0, MigrateOptions migrateOptions = MigrateOptions.None, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.KeyMigrateAsync(key, toServer, toDatabase, timeoutMilliseconds, migrateOptions, flags));


            public Task<bool> KeyMoveAsync(RedisKey key, int database, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.KeyMoveAsync(key, database, flags));


            public Task<bool> KeyPersistAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.KeyPersistAsync(key, flags));


            public Task<RedisKey> KeyRandomAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.KeyRandomAsync(flags));


            public Task<bool> KeyRenameAsync(RedisKey key, RedisKey newKey, When when = When.Always, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.KeyRenameAsync(key, newKey, when, flags));


            public Task KeyRestoreAsync(RedisKey key, byte[] value, TimeSpan? expiry = null, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.KeyRestoreAsync(key, value, expiry, flags));


            public Task<TimeSpan?> KeyTimeToLiveAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.KeyTimeToLiveAsync(key, flags));


            public Task<RedisType> KeyTypeAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.KeyTypeAsync(key, flags));


            public Task<RedisValue> ListGetByIndexAsync(RedisKey key, long index, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListGetByIndexAsync(key, index, flags));


            public Task<long> ListInsertAfterAsync(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListInsertAfterAsync(key, pivot, value, flags));


            public Task<long> ListInsertBeforeAsync(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListInsertBeforeAsync(key, pivot, value, flags));


            public Task<RedisValue> ListLeftPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListLeftPopAsync(key, flags));


            public Task<long> ListLeftPushAsync(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListLeftPushAsync(key, value, when, flags));


            public Task<long> ListLeftPushAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListLeftPushAsync(key, values, flags));


            public Task<long> ListLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListLengthAsync(key, flags));


            public Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListRangeAsync(key, start, stop, flags));


            public Task<long> ListRemoveAsync(RedisKey key, RedisValue value, long count = 0, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListRemoveAsync(key, value, count, flags));


            public Task<RedisValue> ListRightPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListRightPopAsync(key, flags));


            public Task<RedisValue> ListRightPopLeftPushAsync(RedisKey source, RedisKey destination, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListRightPopLeftPushAsync(source, destination, flags));


            public Task<long> ListRightPushAsync(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListRightPushAsync(key, value, when, flags));


            public Task<long> ListRightPushAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListRightPushAsync(key, values, flags));


            public Task ListSetByIndexAsync(RedisKey key, long index, RedisValue value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListSetByIndexAsync(key, index, value, flags));


            public Task ListTrimAsync(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ListTrimAsync(key, start, stop, flags));


            public Task<bool> LockExtendAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.LockExtendAsync(key, value, expiry, flags));


            public Task<RedisValue> LockQueryAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.LockQueryAsync(key, flags));


            public Task<bool> LockReleaseAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.LockReleaseAsync(key, value, flags));


            public Task<bool> LockTakeAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.LockTakeAsync(key, value, expiry, flags));


            public Task<TimeSpan> PingAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.PingAsync(flags));


            public Task<long> PublishAsync(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.PublishAsync(channel, message, flags));


            public Task<RedisResult> ScriptEvaluateAsync(string script, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ScriptEvaluateAsync(script, keys, values, flags));

            public Task<RedisResult> ExecuteAsync(string command, params object[] args) 
                => DoDecoratedOperation(t => t.ExecuteAsync(command, args));

            public Task<RedisResult> ExecuteAsync(string command, ICollection<object> args, CommandFlags flags = CommandFlags.None) 
                => DoDecoratedOperation(t => t.ExecuteAsync(command, args, flags));


            public Task<RedisResult> ScriptEvaluateAsync(byte[] hash, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ScriptEvaluateAsync(hash, keys, values, flags));


            public Task<RedisResult> ScriptEvaluateAsync(LuaScript script, object parameters = null, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ScriptEvaluateAsync(script, parameters, flags));


            public Task<RedisResult> ScriptEvaluateAsync(LoadedLuaScript script, object parameters = null, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.ScriptEvaluateAsync(script, parameters, flags));


            public Task<bool> SetAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetAddAsync(key, value, flags));


            public Task<long> SetAddAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetAddAsync(key, values, flags));


            public Task<long> SetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetCombineAndStoreAsync(operation, destination, first, second, flags));


            public Task<long> SetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetCombineAndStoreAsync(operation, destination, keys, flags));


            public Task<RedisValue[]> SetCombineAsync(SetOperation operation, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetCombineAsync(operation, first, second, flags));


            public Task<RedisValue[]> SetCombineAsync(SetOperation operation, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetCombineAsync(operation, keys, flags));


            public Task<bool> SetContainsAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetContainsAsync(key, value, flags));


            public Task<long> SetLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetLengthAsync(key, flags));


            public Task<RedisValue[]> SetMembersAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetMembersAsync(key, flags));


            public Task<bool> SetMoveAsync(RedisKey source, RedisKey destination, RedisValue value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetMoveAsync(source, destination, value, flags));


            public Task<RedisValue> SetPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetPopAsync(key, flags));


            public Task<RedisValue> SetRandomMemberAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetRandomMemberAsync(key, flags));


            public Task<RedisValue[]> SetRandomMembersAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetRandomMembersAsync(key, count, flags));


            public Task<bool> SetRemoveAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetRemoveAsync(key, value, flags));


            public Task<long> SetRemoveAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SetRemoveAsync(key, values, flags));


            public Task<long> SortAndStoreAsync(RedisKey destination, RedisKey key, long skip = 0, long take = -1, Order order = Order.Ascending, SortType sortType = SortType.Numeric, RedisValue by = default(RedisValue), RedisValue[] get = null, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortAndStoreAsync(destination, key, skip, take, order, sortType, by, get, flags));


            public Task<RedisValue[]> SortAsync(RedisKey key, long skip = 0, long take = -1, Order order = Order.Ascending, SortType sortType = SortType.Numeric, RedisValue by = default(RedisValue), RedisValue[] get = null, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortAsync(key, skip, take, order, sortType, by, get, flags));


            public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score, CommandFlags flags)
                => DoDecoratedOperation(t => t.SortedSetAddAsync(key, member, score, flags));


            public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score, When when = When.Always, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetAddAsync(key, member, score, when, flags));


            public Task<long> SortedSetAddAsync(RedisKey key, SortedSetEntry[] values, CommandFlags flags)
                => DoDecoratedOperation(t => t.SortedSetAddAsync(key, values, flags));


            public Task<long> SortedSetAddAsync(RedisKey key, SortedSetEntry[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetAddAsync(key, values, when, flags));


            public Task<long> SortedSetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second, Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetCombineAndStoreAsync(operation, destination, first, second, aggregate, flags));


            public Task<long> SortedSetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey[] keys, double[] weights = null, Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetCombineAndStoreAsync(operation, destination, keys, weights, aggregate, flags));


            public Task<double> SortedSetDecrementAsync(RedisKey key, RedisValue member, double value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetDecrementAsync(key, member, value, flags));


            public Task<double> SortedSetIncrementAsync(RedisKey key, RedisValue member, double value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetIncrementAsync(key, member, value, flags));


            public Task<long> SortedSetLengthAsync(RedisKey key, double min = double.NegativeInfinity, double max = double.PositiveInfinity, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetLengthAsync(key, min, max, exclude, flags));


            public Task<long> SortedSetLengthByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetLengthByValueAsync(key, min, max, exclude, flags));


            public Task<RedisValue[]> SortedSetRangeByRankAsync(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetRangeByRankAsync(key, start, stop, order, flags));


            public Task<SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetRangeByRankWithScoresAsync(key, start, stop, order, flags));


            public Task<RedisValue[]> SortedSetRangeByScoreAsync(RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetRangeByScoreAsync(key, start, stop, exclude, order, skip, take, flags));


            public Task<SortedSetEntry[]> SortedSetRangeByScoreWithScoresAsync(RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetRangeByScoreWithScoresAsync(key, start, stop, exclude, order, skip, take, flags));


            public Task<RedisValue[]> SortedSetRangeByValueAsync(RedisKey key, RedisValue min = default(RedisValue), RedisValue max = default(RedisValue), Exclude exclude = Exclude.None, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetRangeByValueAsync(key, min, max, exclude, skip, take, flags));


            public Task<long?> SortedSetRankAsync(RedisKey key, RedisValue member, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetRankAsync(key, member, order, flags));


            public Task<bool> SortedSetRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetRemoveAsync(key, member, flags));


            public Task<long> SortedSetRemoveAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetRemoveAsync(key, members, flags));


            public Task<long> SortedSetRemoveRangeByRankAsync(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetRemoveRangeByRankAsync(key, start, stop, flags));


            public Task<long> SortedSetRemoveRangeByScoreAsync(RedisKey key, double start, double stop, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetRemoveRangeByScoreAsync(key, start, stop, exclude, flags));


            public Task<long> SortedSetRemoveRangeByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetRemoveRangeByValueAsync(key, min, max, exclude, flags));


            public Task<double?> SortedSetScoreAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.SortedSetScoreAsync(key, member, flags));


            public Task<long> StringAppendAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringAppendAsync(key, value, flags));


            public Task<long> StringBitCountAsync(RedisKey key, long start = 0, long end = -1, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringBitCountAsync(key, start, end, flags));


            public Task<long> StringBitOperationAsync(Bitwise operation, RedisKey destination, RedisKey first, RedisKey second = default(RedisKey), CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringBitOperationAsync(operation, destination, first, second, flags));


            public Task<long> StringBitOperationAsync(Bitwise operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringBitOperationAsync(operation, destination, keys, flags));


            public Task<long> StringBitPositionAsync(RedisKey key, bool bit, long start = 0, long end = -1, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringBitPositionAsync(key, bit, start, end, flags));


            public Task<long> StringDecrementAsync(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringDecrementAsync(key, value, flags));


            public Task<double> StringDecrementAsync(RedisKey key, double value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringDecrementAsync(key, value, flags));


            public Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringGetAsync(key, flags));


            public Task<RedisValue[]> StringGetAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringGetAsync(keys, flags));


            public Task<bool> StringGetBitAsync(RedisKey key, long offset, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringGetBitAsync(key, offset, flags));


            public Task<RedisValue> StringGetRangeAsync(RedisKey key, long start, long end, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringGetRangeAsync(key, start, end, flags));


            public Task<RedisValue> StringGetSetAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringGetSetAsync(key, value, flags));


            public Task<RedisValueWithExpiry> StringGetWithExpiryAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringGetWithExpiryAsync(key, flags));


            public Task<long> StringIncrementAsync(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringIncrementAsync(key, value, flags));


            public Task<double> StringIncrementAsync(RedisKey key, double value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringIncrementAsync(key, value, flags));


            public Task<long> StringLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringLengthAsync(key, flags));


            public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringSetAsync(key, value, expiry, when, flags));


            public Task<bool> StringSetAsync(KeyValuePair<RedisKey, RedisValue>[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringSetAsync(values, when, flags));


            public Task<bool> StringSetBitAsync(RedisKey key, long offset, bool bit, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringSetBitAsync(key, offset, bit, flags));


            public Task<RedisValue> StringSetRangeAsync(RedisKey key, long offset, RedisValue value, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(t => t.StringSetRangeAsync(key, offset, value, flags));


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
