import { Client } from 'discord.js';
import { CommandRegistry } from './Lib/CommandUtility/CommandRegistry';
import { DotENV } from './Lib/Events/DotENV';
import { Login } from './Lib/Events/Login';
import { Messages } from './Lib/Events/Messages';
import { Logger } from './Lib/Logging/LoggingUtility';
import { SOAPUtil } from './Lib/RCCService/SOAPUtility';
import { GlobalConfig } from './Lib/Utility/GlobalConfig';
import { SystemUtil } from './Lib/Utility/SystemUtil';

Logger.Debug("Process '%s' opened with file name '%s'.", process.pid.toString(16), __filename);

DotENV.GlobalConfigure();

const discordClient = new Client();

GlobalConfig.Bot = discordClient;

discordClient.on('message', Messages.MessageSent);
discordClient.on('ready', Login.OnLogin);

discordClient.login(GlobalConfig.BotToken);

process.stdin.resume();

process.on('SIGINT', () => {
	console.log('Got SIGINT. Will start shutdown procedure within 1 second.');
	setTimeout(() => {
		SystemUtil.KillRccServiceSafe();
		SystemUtil.KillServerSafe();
		Logger.TryClearLogs();
		process.exit(0);
	}, 1000);
});
process.on('SIGUSR1', () => {
	console.log('Got SIGUSR1. Will exit app without closing child processes with 1 second.');
	setTimeout(() => {
		Logger.TryClearLogs(true);
		return process.exit(0);
	}, 1000);
});
process.on('SIGUSR2', () => {
	console.log('Got SIGUSR2. Will close all child processes, and clear LocalLog within 1 second.');
	setTimeout(() => {
		SystemUtil.KillRccServiceSafe();
		SystemUtil.KillServerSafe();
		Logger.TryClearLogs(true);
	}, 1000);
});

process.on('SIGALRM', () => {
	console.log('Alarm clock');
	process.exit(0);
});

process.on('SIGHUP', () => {
	console.log('Hangup');
	process.exit(0);
});

process.on('SIGIO', () => {
	console.log('I/O possible');
	process.exit(0);
});

process.on('SIGPOLL', () => {
	console.log('I/O possible');
	process.exit(0);
});

process.on('SIGPROF', () => {
	console.log('Profiling timer expired');
	process.exit(0);
});

process.on('SIGVTALRM', () => {
	console.log('Virtual timer expired');
	process.exit(0);
});

process.on('SIGPWR', () => {
	console.log('Power failure');
	process.exit(0);
});

process.on('SIGSTKFLT', () => {
	console.log('Stack fault');
	process.exit(0);
});

process.on('SIGKILL', () => {
	console.log('Killed');
	process.exit(0);
});

process.on('SIGKILL', () => {
	console.log('Got SIGPIPE. Ignoring.');
});

process.on('SIGTERM', () => {
	console.log('Got SIGTERM. Will start shutdown procedure within 1 second.');
	setTimeout(() => {
		SystemUtil.KillRccServiceSafe();
		SystemUtil.KillServerSafe();
		Logger.TryClearLogs();
		process.exit(0);
	}, 1000);
});

process.on('uncaughtException', (ex) => {
	console.log('\n***\nPROCESS CRASHED\n***\n');
	console.log('REASON FOR CRASH: %s', ex.message || '');
	process.exit(1);
});

process.on('unhandledRejection', (reason) => {
	console.log('\n***\nPROCESS CRASHED\n***\n');
	console.log('REASON FOR CRASH: %s', reason || '');
	process.exit(1);
});

process.stdin.setRawMode(true);

process.stdin.setEncoding('utf8');

process.stdin.on('data', function (key: string) {
	if (key === '\u0003' || key === '\u001b') {
		return process.emit('SIGINT', 'SIGINT');
	}

	if (key === 'r' || key === 'R') {
		return process.emit('SIGUSR2', 'SIGUSR2');
	}

	if (key === 't' || key === 'T') {
		return Logger.Log("The current time is '%s'", new Date(Date.now()).toUTCString());
	}

	if (key === 'o' || key === 'O') {
		return SOAPUtil.OpenRCC();
	}

	if (key === 'u' || key === 'U') {
		return Logger.Log('The current process uptime is: %ss', process.uptime().toFixed(7));
	}

	if (key === 'e' || key === 'E') {
		return process.emit('SIGUSR1', 'SIGUSR1');
	}

	if (key === 'm' || key === 'M') {
		return CommandRegistry.LogReport();
	}
});
