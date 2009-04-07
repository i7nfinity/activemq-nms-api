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
using System.Xml.Schema;
using System.Xml.Serialization;
using Apache.NMS.Util;
using NUnit.Framework;

namespace Apache.NMS.Test
{
	// Serializable object.  In real life, this type of object would be generated from
	// an XSD file using XSDObjectGen.
	[Serializable]
	public enum CheckType
	{
		// [XmlEnum(Name = "message")]
		message,
		// [XmlEnum(Name = "command")]
		command,
		// [XmlEnum(Name = "response")]
		response
	}

	[XmlRoot(ElementName = "NMSTestXmlType1", IsNullable = false), Serializable]
	public class NMSTestXmlType1
	{
		// [XmlElement(ElementName = "crcCheck", IsNullable = false, DataType = "int")]
		public int crcCheck;

		// [XmlElement(Type = typeof(CheckType), ElementName = "checkType", IsNullable = false)]
		public CheckType checkType;

		public NMSTestXmlType1()
		{
			crcCheck = 0;
			checkType = CheckType.message;
		}
	}

	[XmlRoot(ElementName = "NMSTestXmlType2", IsNullable = false), Serializable]
	public class NMSTestXmlType2
	{
		// [XmlElement(ElementName = "stringCheck", IsNullable = false, DataType = "string")]
		public string stringCheck;

		public NMSTestXmlType2()
		{
			stringCheck = String.Empty;
		}
	}

	[TestFixture]
	public class XmlMessageTest : NMSTestSupport
	{
		protected static string DESTINATION_NAME = "XmlMessageDestination";
		protected static string TEST_CLIENT_ID = "XmlMessageClientId";

		[Test]
		public void SendReceiveXmlMessage()
		{
			using(IConnection connection = CreateConnection(TEST_CLIENT_ID))
			{
				connection.Start();
				using(ISession session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge))
				{
					IDestination destination = SessionUtil.GetDestination(session, DESTINATION_NAME);
					using(IMessageConsumer consumer = session.CreateConsumer(destination))
					using(IMessageProducer producer = session.CreateProducer(destination))
					{
						producer.RequestTimeout = receiveTimeout;

						NMSTestXmlType1 srcIntObject = new NMSTestXmlType1();
						srcIntObject.crcCheck = 0xbadf00d;
						srcIntObject.checkType = CheckType.command;
						producer.Send(NMSConvert.ToXmlMessage(session, srcIntObject));

						NMSTestXmlType2 srcStringObject = new NMSTestXmlType2();
						srcStringObject.stringCheck = "BadFood";
						producer.Send(NMSConvert.ToXmlMessage(session, srcStringObject));

						// Demonstrate the ability to generically handle multiple object types
						// sent to the same consumer.  If only one object type is ever sent to
						// the destination, then a simple inline cast is all that is necessary
						// when calling the NMSConvert.FromXmlMessage() function.

						for(int index = 0; index < 2; index++)
						{
							object receivedObject = NMSConvert.FromXmlMessage(consumer.Receive(receiveTimeout));
							Assert.IsNotNull(receivedObject, "Failed to retrieve XML message object.");

							if(receivedObject is NMSTestXmlType1)
							{
								NMSTestXmlType1 destObject = (NMSTestXmlType1) receivedObject;
								Assert.AreEqual(srcIntObject.crcCheck, destObject.crcCheck, "CRC integer mis-match.");
								Assert.AreEqual(srcIntObject.checkType, destObject.checkType, "Check type mis-match.");
							}
							else if(receivedObject is NMSTestXmlType2)
							{
								NMSTestXmlType2 destObject = (NMSTestXmlType2) receivedObject;
								Assert.AreEqual(srcStringObject.stringCheck, destObject.stringCheck, "CRC string mis-match.");
							}
							else
							{
								Assert.Fail("Invalid object type.");
							}
						}
					}
				}
			}
		}
	}
}