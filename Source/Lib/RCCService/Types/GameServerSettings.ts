export class GameServerSettings {
	public PlaceId: long;
	public UniverseId: long;
	public MatchmakingContextId: int;
	public JobSignature: string;
	public GameCode: string;
	public BaseUrl: string;
	public GameId: string;
	public MachineAddress: string;
	public ServerId: int;
	public GsmInterval: int;
	public MaxPlayers: int;
	public MaxGameInstances: int;
	public ApiKey: string;
	public PreferredPlayerCapacity: int;
	public PlaceVisitAccessKey: string;
	public DatacenterId: int;
	public CreatorId: long;
	public CreatorType: string;
	public PlaceVersion: int;
	public VipOwnerId?: long;
	public PlaceFetchUrl: string;
	public Metadata: string;

	public constructor(
		placeId: long,
		universeId: long,
		matchmakingId: int,
		jobSignature: string,
		gameCode: string,
		baseUrl: string,
		gameId: string,
		machineAddress: string,
		serverId: int,
		gsmInterval: int,
		maxPlayers: int,
		maxGameInstances: int,
		apiKey: string,
		preferredPlayerCapacity: int,
		placeVisitAccessKey: string,
		datacenterId: int,
		creatorId: long,
		creatorType: string,
		placeVersion: int,
		vipOwnerId: long,
		placeFetchUrl: string,
		metadata: string,
	) {
		this.PlaceId = placeId;
		this.UniverseId = universeId;
		this.MatchmakingContextId = matchmakingId;
		this.JobSignature = jobSignature;
		this.GameCode = gameCode;
		this.BaseUrl = baseUrl;
		this.GameId = gameId;
		this.MachineAddress = machineAddress;
		this.ServerId = serverId;
		this.GsmInterval = gsmInterval;
		this.MaxPlayers = maxPlayers;
		this.MaxGameInstances = maxGameInstances;
		this.ApiKey = apiKey;
		this.PreferredPlayerCapacity = preferredPlayerCapacity;
		this.PlaceVisitAccessKey = placeVisitAccessKey;
		this.DatacenterId = datacenterId;
		this.CreatorId = creatorId;
		this.CreatorType = creatorType;
		this.PlaceVersion = placeVersion;
		this.VipOwnerId = vipOwnerId;
		this.PlaceFetchUrl = placeFetchUrl;
		this.Metadata = metadata;
	}
}
