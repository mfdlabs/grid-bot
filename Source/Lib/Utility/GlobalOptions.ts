import { Method, AxiosRequestConfig } from 'axios';

export class GlobalOptions implements AxiosRequestConfig {
	public static url: string = 'http://127.0.0.1:64989';
	public static method: Method = 'POST';
	public static headers: {
		Accept: 'text/xml';
		'Cache-Control': 'no-cache';
		Pragma: 'no-cache';
		SOAPAction: 'Execute';
	};
}
