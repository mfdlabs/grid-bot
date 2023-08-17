namespace Redis;

using System.Collections.Generic;

using StackExchange.Redis;

/// <summary>
/// Extension methods for Redis Paged Operations.
/// </summary>
public static class RedisPagedOperator
{
    /// <summary>
    /// Delete sorted set entries paged.
    /// </summary>
    /// <param name="redisClient">The client.</param>
    /// <param name="key">The key.</param>
    /// <param name="inclusiveMinScore">The min score.</param>
    /// <param name="inclusiveMaxScore">The max score.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="deletionCommandFlags">The command flags.</param>
    public static void DeleteSortedSetEntriesPaged(
        this IRedisClient redisClient, 
        string key, 
        double inclusiveMinScore,
        double inclusiveMaxScore,
        int pageSize, 
        CommandFlags deletionCommandFlags
    )
    {
        foreach (var (start, stop) in redisClient.GetSortedSetRanges(key, inclusiveMinScore, inclusiveMaxScore, pageSize))
            redisClient.Execute(
                key, 
                db => db.SortedSetRemoveRangeByScore(
                    key: key, 
                    start: start, 
                    stop: stop, 
                    flags: deletionCommandFlags
                )
            );
    }

    private static IReadOnlyCollection<(double start, double stop)> GetSortedSetRanges(
        this IRedisClient redisClient, 
        string key, 
        double inclusiveMinScore, 
        double inclusiveMaxScore,
        int pageSize
    )
    {
        long page = 0;
        double stopScoreToFetchUpto = inclusiveMaxScore;
        double? score = null;

        var ranges = new List<(double start, double stop)>();

        SortedSetEntry[] scores;
        do
        {
            var startIndex = page;

            if (score == null)
            {
                scores = redisClient.Execute(
                    key, 
                    db => db.SortedSetRangeByScoreWithScores(
                        key: key, 
                        start: inclusiveMinScore, 
                        stop: stopScoreToFetchUpto, 
                        skip: startIndex, 
                        take: 1
                    )
                );

                if (scores == null || scores.Length == 0)
                    break;

                score = scores[0].Score;
            }

            scores = redisClient.Execute(
                key, 
                db => db.SortedSetRangeByScoreWithScores(
                    key: key, 
                    start: inclusiveMinScore, 
                    stop: stopScoreToFetchUpto,
                    skip: startIndex + pageSize,
                    take: 1
                )
            );

            var maxScore = inclusiveMaxScore;
            if (scores != null && scores.Length != 0)
                maxScore = scores[0].Score;

            ranges.Add((score.Value, maxScore));

            page += pageSize;
            score = maxScore;
        }
        while (scores?.Length != 0);

        return ranges;
    }
}
