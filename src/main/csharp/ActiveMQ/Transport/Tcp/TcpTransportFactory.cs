/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Net;
using System.Net.Sockets;
using ActiveMQ.Commands;
using ActiveMQ.OpenWire;
using ActiveMQ.Transport;
using ActiveMQ.Util;

namespace ActiveMQ.Transport.Tcp {
        public class TcpTransportFactory : ITransportFactory
        {
                private bool useLogging = false;

                public bool UseLogging {
                        get { return useLogging; } 
                        set { useLogging = value; } 
                }

                public ITransport CreateTransport(Uri location)
                {
                        // Extract query parameters from broker Uri
                        System.Collections.Specialized.StringDictionary map = URISupport.ParseQuery(location.Query);

                        // Set transport. properties on this (the factory)
                        URISupport.SetProperties(this, map, "transport.");

                        // Console.WriteLine("Opening socket to: " + host + " on port: " + port);
                        Socket socket = Connect(location.Host, location.Port);
                        TcpTransport tcpTransport = new TcpTransport(socket);
                        ITransport rc = tcpTransport;

                        if (UseLogging)
                        {
                                rc = new LoggingTransport(rc);
                        }

                        // Set wireformat. properties on the wireformat owned by the tcpTransport
                        URISupport.SetProperties(tcpTransport.Wireformat.PreferedWireFormatInfo, map, "wireFormat.");

                        rc = new WireFormatNegotiator(rc, tcpTransport.Wireformat);
                        rc = new ResponseCorrelator(rc);
                        rc = new MutexTransport(rc);

                        return rc;
                }

                protected Socket Connect(string host, int port)
                {
                        // Looping through the AddressList allows different type of connections to be tried
                        // (IPv4, IPv6 and whatever else may be available).
                        IPHostEntry hostEntry = Dns.Resolve(host);
                        foreach (IPAddress address in hostEntry.AddressList)
                        {
                                Socket socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                                socket.Connect(new IPEndPoint(address, port));
                                if (socket.Connected)
                                {
                                        return socket;
                                }
                        }
                        throw new SocketException();
                }
        }
}
