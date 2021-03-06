﻿using System;
using System.IO;
using Moq;
using MountainWarehouse.EasyMWS;
using MountainWarehouse.EasyMWS.Client;
using MountainWarehouse.EasyMWS.Enums;
using MountainWarehouse.EasyMWS.Helpers;
using MountainWarehouse.EasyMWS.Logging;
using MountainWarehouse.EasyMWS.Model;
using MountainWarehouse.EasyMWS.Processors;
using MountainWarehouse.EasyMWS.Services;
using NUnit.Framework;

namespace EasyMWS.Tests
{
	public class EasyMwsClientTests
	{
		private AmazonRegion _region = AmazonRegion.Europe;
		private EasyMwsClient _easyMwsClient;
		private Mock<IEasyMwsLogger> _loggerMock;

		private Mock<IFeedQueueingProcessor> _feedProcessorMock;
		private Mock<IReportQueueingProcessor> _reportProcessorMock;

		[SetUp]
		public void SetUp()
		{
			_feedProcessorMock = new Mock<IFeedQueueingProcessor>();
			_reportProcessorMock = new Mock<IReportQueueingProcessor>();
			_loggerMock = new Mock<IEasyMwsLogger>();
			_easyMwsClient = new EasyMwsClient(AmazonRegion.Europe, "MerchantId", "test", "test", _reportProcessorMock.Object,
				_feedProcessorMock.Object, _loggerMock.Object, new EasyMwsOptions());
		}

		// feed processor tests

		// report processor tests

		[Test]
		public void InitializingClient_WithNullMerchant_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() =>
				_easyMwsClient = new EasyMwsClient(AmazonRegion.Europe, null, "test", "test", _reportProcessorMock.Object,
					_feedProcessorMock.Object, _loggerMock.Object, new EasyMwsOptions()));
		}
		[Test]
		public void InitializingClient_WithEmptyMerchant_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() =>
				_easyMwsClient = new EasyMwsClient(AmazonRegion.Europe, string.Empty, "test", "test", _reportProcessorMock.Object,
					_feedProcessorMock.Object, _loggerMock.Object, new EasyMwsOptions()));
		}

		[Test]
		public void InitializingClient_WithNullAccessKeyId_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() =>
				_easyMwsClient = new EasyMwsClient(AmazonRegion.Europe, "MerchantId", null, "test", _reportProcessorMock.Object,
					_feedProcessorMock.Object, _loggerMock.Object, new EasyMwsOptions()));
		}

		[Test]
		public void InitializingClient_WithEmptyAccessKeyId_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() =>
				_easyMwsClient = new EasyMwsClient(AmazonRegion.Europe, "MerchantId", string.Empty, "test", _reportProcessorMock.Object,
					_feedProcessorMock.Object, _loggerMock.Object, new EasyMwsOptions()));
		}

		[Test]
		public void InitializingClient_WithNullMwsSecretAccessKey_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() =>
				_easyMwsClient = new EasyMwsClient(AmazonRegion.Europe, "MerchantId", "test", null, _reportProcessorMock.Object,
					_feedProcessorMock.Object, _loggerMock.Object, new EasyMwsOptions()));
		}
		[Test]
		public void InitializingClient_WithEmptyMwsSecretAccessKey_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() =>
				_easyMwsClient = new EasyMwsClient(AmazonRegion.Europe, "MerchantId", "test", string.Empty, _reportProcessorMock.Object,
					_feedProcessorMock.Object, _loggerMock.Object, new EasyMwsOptions()));
		}

		[Test]
		public void Poll_CallsReportProcessorPollMethod_Once()
		{
			_easyMwsClient.Poll();

			_reportProcessorMock.Verify(rpm => rpm.PollReports(It.IsAny<IReportRequestEntryService>()), Times.Once);
		}

		[Test]
		public void Poll_CallsFeedProcessorPollMethod_Once()
		{
			_easyMwsClient.Poll();

			_feedProcessorMock.Verify(fpm => fpm.PollFeeds(It.IsAny<IFeedSubmissionEntryService>()), Times.Once);
		}

		[Test]
		public void QueueReport_CallsReportProcessorQueueMethod_Once()
		{
			_easyMwsClient.QueueReport(new ReportRequestPropertiesContainer("testReportType", ContentUpdateFrequency.Unknown),
				new Action<Stream, object>((
					(stream, o) => { })), new { });

			_reportProcessorMock.Verify(rpm => rpm.QueueReport(It.IsAny<IReportRequestEntryService>(),It.IsAny<ReportRequestPropertiesContainer>(), It.IsAny<Action<Stream, object>>(), It.IsAny<object>()), Times.Once);
		}

		[Test]
		public void QueueFeed_CallsFeedProcessorQueueMethod_Once()
		{
			_easyMwsClient.QueueFeed(new FeedSubmissionPropertiesContainer("testFeedContent", "testFeedType"),
				new Action<Stream, object>((
					(stream, o) => { })), new { });

			_feedProcessorMock.Verify(rpm => rpm.QueueFeed(It.IsAny<IFeedSubmissionEntryService>(), It.IsAny<FeedSubmissionPropertiesContainer>(), It.IsAny<Action<Stream, object>>(), It.IsAny<object>()), Times.Once);
		}
	}
}