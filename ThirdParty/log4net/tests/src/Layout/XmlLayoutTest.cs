#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.IO;
using System.Xml;

using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;

using NUnit.Framework;

namespace log4net.Tests.Layout
{
	[TestFixture]
	public class XmlLayoutTest
	{
		/// <summary>
		/// Build a basic <see cref="LoggingEventData"/> object with some default values.
		/// </summary>
		/// <returns>A useful LoggingEventData object</returns>
		private LoggingEventData CreateBaseEvent()
		{
			LoggingEventData ed = new LoggingEventData();
			ed.Domain = "Tests";
			ed.ExceptionString = "";
			ed.Identity = "TestRunner";
			ed.Level = Level.Info;
			ed.LocationInfo = new LocationInfo(GetType());
			ed.LoggerName = "TestLogger";
			ed.Message = "Test message";
			ed.ThreadName = "TestThread";
			ed.TimeStamp = DateTime.Today;
			ed.UserName = "TestRunner";
			ed.Properties = new PropertiesDictionary();

			return ed;
		}

		private static string CreateEventNode(string message)
		{
			return String.Format("<event logger=\"TestLogger\" timestamp=\"{0}\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message>{1}</message></event>" + Environment.NewLine,
#if NET_2_0 || MONO_2_0
			                     XmlConvert.ToString(DateTime.Today, XmlDateTimeSerializationMode.Local),
#else
			                     XmlConvert.ToString(DateTime.Today),
#endif
			                     message);
		}

		private static string CreateEventNode(string key, string value)
		{
			return String.Format("<event logger=\"TestLogger\" timestamp=\"{0}\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"><message>Test message</message><properties><data name=\"{1}\" value=\"{2}\" /></properties></event>" + Environment.NewLine,
#if NET_2_0 || MONO_2_0
			                     XmlConvert.ToString(DateTime.Today, XmlDateTimeSerializationMode.Local),
#else
			                     XmlConvert.ToString(DateTime.Today),
#endif
			                     key,
			                     value);
		}

		[Test]
		public void TestBasicEventLogging()
		{
			TextWriter writer = new StringWriter();
			XmlLayout layout = new XmlLayout();
			LoggingEventData evt = CreateBaseEvent();

			layout.Format(writer, new LoggingEvent(evt));

			string expected = CreateEventNode("Test message");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestIllegalCharacterMasking()
		{
			TextWriter writer = new StringWriter();
			XmlLayout layout = new XmlLayout();
			LoggingEventData evt = CreateBaseEvent();

			evt.Message = "This is a masked char->\uFFFF";

			layout.Format(writer, new LoggingEvent(evt));

			string expected = CreateEventNode("This is a masked char-&gt;?");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestCDATAEscaping1()
		{
			TextWriter writer = new StringWriter();
			XmlLayout layout = new XmlLayout();
			LoggingEventData evt = CreateBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message = "&&&&&&&Escape this ]]>. End here.";

			layout.Format(writer, new LoggingEvent(evt));

			string expected = CreateEventNode("<![CDATA[&&&&&&&Escape this ]]>]]<![CDATA[>. End here.]]>");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestCDATAEscaping2()
		{
			TextWriter writer = new StringWriter();
			XmlLayout layout = new XmlLayout();
			LoggingEventData evt = CreateBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message = "&&&&&&&Escape the end ]]>";

			layout.Format(writer, new LoggingEvent(evt));

			string expected = CreateEventNode("<![CDATA[&&&&&&&Escape the end ]]>]]&gt;");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestCDATAEscaping3()
		{
			TextWriter writer = new StringWriter();
			XmlLayout layout = new XmlLayout();
			LoggingEventData evt = CreateBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message = "]]>&&&&&&&Escape the begining";

			layout.Format(writer, new LoggingEvent(evt));

			string expected = CreateEventNode("<![CDATA[]]>]]<![CDATA[>&&&&&&&Escape the begining]]>");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestBase64EventLogging()
		{
			TextWriter writer = new StringWriter();
			XmlLayout layout = new XmlLayout();
			LoggingEventData evt = CreateBaseEvent();

			layout.Base64EncodeMessage = true;
			layout.Format(writer, new LoggingEvent(evt));

			string expected = CreateEventNode("VGVzdCBtZXNzYWdl");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestPropertyEventLogging()
		{
			LoggingEventData evt = CreateBaseEvent();
			evt.Properties["Property1"] = "prop1";

			XmlLayout layout = new XmlLayout();
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = CreateEventNode("Property1", "prop1");

			Assert.AreEqual(expected, stringAppender.GetString());
		}

		[Test]
		public void TestBase64PropertyEventLogging()
		{
			LoggingEventData evt = CreateBaseEvent();
			evt.Properties["Property1"] = "prop1";

			XmlLayout layout = new XmlLayout();
			layout.Base64EncodeProperties = true;
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = CreateEventNode("Property1", "cHJvcDE=");

			Assert.AreEqual(expected, stringAppender.GetString());
		}

		[Test]
		public void TestPropertyCharacterEscaping()
		{
			LoggingEventData evt = CreateBaseEvent();
			evt.Properties["Property1"] = "prop1 \"quoted\"";

			XmlLayout layout = new XmlLayout();
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = CreateEventNode("Property1", "prop1 &quot;quoted&quot;");

			Assert.AreEqual(expected, stringAppender.GetString());
		}

		[Test]
		public void TestPropertyIllegalCharacterMasking()
		{
			LoggingEventData evt = CreateBaseEvent();
			evt.Properties["Property1"] = "mask this ->\uFFFF";

			XmlLayout layout = new XmlLayout();
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = CreateEventNode("Property1", "mask this -&gt;?");

			Assert.AreEqual(expected, stringAppender.GetString());
		}

		[Test]
		public void TestPropertyIllegalCharacterMaskingInName()
		{
			LoggingEventData evt = CreateBaseEvent();
			evt.Properties["Property\uFFFF"] = "mask this ->\uFFFF";

			XmlLayout layout = new XmlLayout();
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = CreateEventNode("Property?", "mask this -&gt;?");

			Assert.AreEqual(expected, stringAppender.GetString());
		}
	}
}