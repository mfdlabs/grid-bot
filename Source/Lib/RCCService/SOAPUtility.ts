import HTTP from 'axios';
import { GlobalOptions } from '../Utility/GlobalOptions';
import xml_parser from 'fast-xml-parser';
import { GlobalConfig } from '../Utility/GlobalConfig';
import { execSync } from 'child_process';
import { Logger } from '../Logging/LoggingUtility';
import { Job } from './Types/SOAPTypes/Job';
import { ScriptExecution } from './Types/SOAPTypes/ScriptExecution';
import { LuaValue } from './Types/SOAPTypes/LuaValue';
import { LuaType } from './Types/SOAPTypes/LuaType';
import { Status } from './Types/SOAPTypes/Status';
import { SoapException } from '../Exceptions/SoapException';
import { TransportException } from '../Exceptions/TransportException';

/**
 * The length of this sucks ass, please shorten it.
 */

export class SOAPUtil {
	public static async HelloWorld(): Promise<string> {
		return await SOAPUtil.CallToService<string>('HelloWorld');
	}

	public static async GetVersion(): Promise<string> {
		return await SOAPUtil.CallToService<string>('GetVersion');
	}

	public static async GetStatus(): Promise<Status> {
		return await SOAPUtil.CallToService<Status>('GetStatus');
	}

	public static async CloseJob(jobID: string): Promise<void> {
		await SOAPUtil.CallToService('CloseJob', { jobID });
	}

	public static async CloseAllJobs(): Promise<void> {
		await SOAPUtil.CallToService('CloseAllJobs');
	}

	public static async CloseExpiredJobs(): Promise<void> {
		await SOAPUtil.CallToService('CloseExpiredJobs');
	}

	public static async GetExpiration(jobID: string): Promise<double> {
		return await SOAPUtil.CallToService<double>('GetExpiration', { jobID });
	}

	public static async RenewLease(jobID: string, expirationInSeconds: double): Promise<double> {
		return await SOAPUtil.CallToService<double>('RenewLease', { jobID, expirationInSeconds });
	}

	public static async GetAllJobs(): Promise<Job[]> {
		return await SOAPUtil.CallToService<Job[]>('GetAllJobs');
	}

	public static async OpenJob(job: Job, script: ScriptExecution): Promise<LuaValue[]> {
		let result = await SOAPUtil.CallToService<LuaValue[]>('OpenJob', { job, script });

		if (result !== undefined) {
			result = await SOAPUtil.ConvertStringTypesToLuaTypes(result);
			return result;
		}

		return [];
	}

	public static async BatchJob(job: Job, script: ScriptExecution): Promise<LuaValue[]> {
		let result = await SOAPUtil.CallToService<LuaValue[]>('BatchJob', { job, script });

		if (result !== undefined) {
			result = await SOAPUtil.ConvertStringTypesToLuaTypes(result);
			return result;
		}

		return [];
	}

	public static async Execute(jobID: string, script: ScriptExecution): Promise<LuaValue[]> {
		let result = await SOAPUtil.CallToService<LuaValue[]>('Execute', { jobID, script });

		if (result !== undefined) {
			result = await SOAPUtil.ConvertStringTypesToLuaTypes(result);
			return result;
		}

		return [];
	}

	public static async Diag(type: int, jobID: string): Promise<LuaValue[]> {
		let result = await SOAPUtil.CallToService<LuaValue[]>('Diag', { jobID, type });

		if (result !== undefined) {
			result = await SOAPUtil.ConvertStringTypesToLuaTypes(result);
			return result;
		}

		return [];
	}

	private static async ConvertStringTypesToLuaTypes(luaValues: LuaValue | LuaValue[]): Promise<LuaValue[]> {
		return new Promise((resumeFunction) => {
			let response = [];
			if (Array.isArray(luaValues))
				luaValues.forEach((v) => {
					response.push({ ...v, type: LuaType[v['type']] || LuaType.LUA_TNIL });
				});
			else response.push({ ...luaValues, type: LuaType[luaValues['type']] || LuaType.LUA_TNIL });

			return resumeFunction(response);
		});
	}

	private static BaseXml(innerXml: string) {
		return `<?xml version="1.0" encoding="UTF - 8"?>
		<SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/" xmlns:SOAP-ENC="http://schemas.xmlsoap.org/soap/encoding/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:ns2="http://roblox.com/RCCService" xmlns:ns1="http://roblox.com/" xmlns:ns3="http://roblox.com/RCCService12"><SOAP-ENV:Body>
		${innerXml}
		</SOAP-ENV:Body></SOAP-ENV:Envelope>`;
	}

	private static async CallToService<T>(action: string, args: Record<string, any> = null): Promise<T> {
		return new Promise(async (resumeFunction, errorFunction) => {
			Logger.Log("'%s' Request to service", action);
			const json = {
				[`ns1:${action}`]: await SOAPUtil.ParseArgs(args),
			};

			const xml = new xml_parser.j2xParser({}).parse(json);

			let response: { [x: string]: { [x: string]: any } };

			const time = Date.now();

			try {
				response = await SOAPUtil.ExecuteRequest(`/${action}`, SOAPUtil.BaseXml(xml));
				Logger.Debug("Took %fs to execute '%s' HttpRequest to Service.", (Date.now() - time) / 1000, action);
			} catch (e) {
				Logger.Debug("Took %fs to execute '%s' HttpRequest to Service.", (Date.now() - time) / 1000, action);
				return errorFunction(e);
			}

			const result = response[`ns1:${action}Response`][`ns1:${action}Result`];

			let data;

			let parsingArray = false;

			if (result !== undefined) {
				if (Array.isArray(result)) {
					let array = [];
					parsingArray = true;
					result.forEach(async (value, idx) => {
						const obj = await SOAPUtil.ParseObjectNoNS1(value);
						(<any[]>(<unknown>array)).push(obj);
						if (idx === result.length - 1) return resumeFunction(<any>array);
					});
				} else if (typeof result === 'object') {
					data = await SOAPUtil.ParseObjectNoNS1(result);
				} else {
					data = result;
				}
			}
			if (!parsingArray) return resumeFunction(data);
		});
	}

	private static async ParseArgs(args: Record<string, any>) {
		return new Promise((resumeFunction) => {
			if (args === null) return resumeFunction({});
			const map = new Map(Object.entries(args));

			let json = {};

			map.forEach(async (v, k) => {
				if (typeof v === 'object') {
					json[`ns1:${k}`] = await SOAPUtil.ParseObject(v);
				} else {
					json[`ns1:${k}`] = v;
				}
			});

			return resumeFunction(json);
		});
	}

	private static async ParseObject(object: Record<string, any>) {
		return new Promise((resumeFunction) => {
			const map = new Map(Object.entries(object));

			let json = {};

			map.forEach(async (v, k) => {
				if (typeof v === 'object') {
					json[`ns1:${k}`] = await SOAPUtil.ParseObject(v);
				} else {
					json[`ns1:${k}`] = v;
				}
			});

			return resumeFunction(json);
		});
	}

	private static async ParseObjectNoNS1(object: Record<string, any>) {
		return new Promise((resumeFunction) => {
			const map = new Map(Object.entries(object));

			let json = {};

			map.forEach(async (v, k) => {
				if (typeof v === 'object') {
					json[k.replace('ns1:', '')] = await SOAPUtil.ParseObjectNoNS1(v);
				} else {
					json[k.replace('ns1:', '')] = v;
				}
			});

			return resumeFunction(json);
		});
	}

	private static ExecuteRequest(url: string, xml: string): Promise<any> {
		return new Promise(async (resumeFunction, errorFunction) => {
			try {
				const response = await HTTP.request({
					...GlobalOptions,
					data: xml,
					url: GlobalOptions.url + url,
					headers: { 'Content-Type': 'text/xml' },
				});
				let parsedData: any;
				try {
					parsedData = xml_parser.parse(response.data);
				} catch (e) {
					errorFunction(new SoapException(e.message));
				}
				return resumeFunction(parsedData['SOAP-ENV:Envelope']['SOAP-ENV:Body']);
			} catch (err) {
				if (err.response !== undefined) {
					let parsedData: any;
					try {
						parsedData = xml_parser.parse(err.response.data);
					} catch (e) {
						errorFunction(new SoapException(err.message));
					}

					if (parsedData['SOAP-ENV:Envelope'] !== undefined) {
						let error =
							parsedData['SOAP-ENV:Envelope']['SOAP-ENV:Fault'] ||
							parsedData['SOAP-ENV:Envelope']['SOAP-ENV:Body']['SOAP-ENV:Fault'];

						if (error !== undefined) {
							return errorFunction(
								new SoapException(
									`${
										GlobalConfig.VerboseErrors
											? `SOAPUtil.ExecuteRequest(${url}, any) ${__filename}: An error occurred from RCCService (Context: ${error['faultcode']})`
											: 'Error occurred'
									}: ${error['faultstring']}`,
								),
							);
						}
					}
				}

				if (GlobalConfig.LaunchRCCIfNotOpen) {
					SOAPUtil.OpenRCC();
					return await SOAPUtil.ExecuteRequest(url, xml)
						.then((e) => resumeFunction(e))
						.catch((err) => errorFunction(err));
				}

				if (err.errno === -4078) {
					return errorFunction(
						new TransportException(
							`${
								GlobalConfig.VerboseErrors
									? `SOAPUtil.ExecuteRequest(${url}, any) ${__filename}: An error occurred from RCCService, connection failed`
									: 'Failed to dispatch request to RCCService'
							}, as it is most likely down.`,
						),
					);
				}
				errorFunction(err);
			}
		});
	}

	// private static ParseLuaValues(result: any) {
	// 	let luaValues: LuaValue[] = [];
	// 	if (result === undefined) return luaValues;

	// 	if (Array.isArray(result)) {
	// 		result.forEach((value) => {
	// 			luaValues.push({
	// 				type: <LuaType>(<unknown>LuaType[value['ns1:type']]) || LuaType.LUA_TNIL,
	// 				value: value['ns1:value'] || null,
	// 				table: value['ns1:table'] || null,
	// 			});
	// 		});
	// 	} else if (Array.isArray(result['ns1:LuaValue'])) {
	// 		result['ns1:LuaValue'].forEach((value) => {
	// 			luaValues.push({
	// 				type: <LuaType>(<unknown>LuaType[value['ns1:type']]) || LuaType.LUA_TNIL,
	// 				value: value['ns1:value'] || null,
	// 				table: value['ns1:table'] || null,
	// 			});
	// 		});
	// 	} else {
	// 		const value = result['ns1:LuaValue'];
	// 		luaValues.push({
	// 			type: <LuaType>(<unknown>LuaType[value['ns1:type']]) || LuaType.LUA_TNIL,
	// 			value: value['ns1:value'] || null,
	// 			table: value['ns1:table'] || null,
	// 		});
	// 	}
	// 	return luaValues;
	// }

	public static LuaTypeToString(luaType: LuaType) {
		switch (luaType) {
			case LuaType.LUA_TBOOLEAN:
				return 'LUA_TBOOLEAN';
			case LuaType.LUA_TNIL:
				return 'LUA_TNIL';
			case LuaType.LUA_TNUMBER:
				return 'LUA_TNUMBER';
			case LuaType.LUA_TSTRING:
				return 'LUA_TSTRING';
			case LuaType.LUA_TTABLE:
				return 'LUA_TTABLE';
		}
	}

	public static OpenRCC() {
		const time = Date.now();

		Logger.Log('Try open RCCService');
		try {
			execSync(GlobalConfig.Root + '\\Bin\\Roblox.Grid.Deployer.exe');
			Logger.Debug('Took %fs to open RCCService via Roblox.Grid.Deployer.exe', (Date.now() - time) / 1000);
			Logger.Info('Successfully opened RCCService via Roblox.Grid.Deployer.exe');
		} catch (e) {
			Logger.Debug('Took %fs to open RCCService via Roblox.Grid.Deployer.exe', (Date.now() - time) / 1000);

			Logger.Error(e.stack);
		}
	}
}
