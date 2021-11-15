// -----------------------------------------------------------------------
//  <copyright file="ServiceIdentity.cs" company="G-Research Limited">
//    Copyright 2020 G-Research Limited
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//  </copyright>
// -----------------------------------------------------------------------

namespace MFDLabs.Hashicorp.ConsulClient
{
    /// <summary>
    /// ServiceIdentity represents a service identity in Consul
    /// </summary>
    public class ServiceIdentity
    {
        public ServiceIdentity()
            : this(string.Empty, new string[] { })
        {
        }

        public ServiceIdentity(string serviceName, string[] datacenters)
        {
            ServiceName = serviceName;
            Datacenters = datacenters;
        }

        public string ServiceName { get; set; }
        public string[] Datacenters { get; set; }
    }

}