namespace MFDLabs.Grid.AutoDeployer
{
	[System.ComponentModel.RunInstaller(true)]
	public class AutoDeployerInstaller : MFDLabs.Wcf.ServiceHostInstaller
	{
		public override string Description => "Polls Github Cloud or Github Enterprise constantly for new releases to deploy to the host machine.";
		public override string DisplayName => "MFDLABS Grid Bot Auto Deployer";
		public override string ServiceName => "MFDLabs.Grid.AutoDeployer";
	}
}
