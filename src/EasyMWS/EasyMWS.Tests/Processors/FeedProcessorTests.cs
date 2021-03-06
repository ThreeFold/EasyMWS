﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Moq;
using MountainWarehouse.EasyMWS;
using MountainWarehouse.EasyMWS.CallbackLogic;
using MountainWarehouse.EasyMWS.Data;
using MountainWarehouse.EasyMWS.Enums;
using MountainWarehouse.EasyMWS.Helpers;
using MountainWarehouse.EasyMWS.Logging;
using MountainWarehouse.EasyMWS.Model;
using MountainWarehouse.EasyMWS.Processors;
using MountainWarehouse.EasyMWS.Services;
using Newtonsoft.Json;
using NUnit.Framework;
using MountainWarehouse.EasyMWS.WebService.MarketplaceWebService;

namespace EasyMWS.Tests.ReportProcessors
{
	public class FeedProcessorTests
	{
		private FeedProcessor _feedProcessor;
		private Mock<IFeedSubmissionEntryService> _feedSubmissionServiceMock;
		private Mock<IMarketplaceWebServiceClient> _marketplaceWebServiceClientMock;
		private Mock<IFeedSubmissionProcessor> _feedSubmissionProcessorMock;
		private Mock<ICallbackActivator> _callbackActivatorMock;
		private Mock<IEasyMwsLogger> _loggerMock;
		private static bool _called;
		private readonly AmazonRegion _amazonRegion = AmazonRegion.Europe;
		private readonly string _merchantId = "testMerchantId1";
		private bool MarkEntryAsHandled = true;

		[SetUp]
		public void SetUp()
		{
			var options = new EasyMwsOptions();
			_feedSubmissionServiceMock = new Mock<IFeedSubmissionEntryService>();
			_marketplaceWebServiceClientMock = new Mock<IMarketplaceWebServiceClient>();
			_feedSubmissionProcessorMock = new Mock<IFeedSubmissionProcessor>();
			_callbackActivatorMock = new Mock<ICallbackActivator>();
			_loggerMock = new Mock<IEasyMwsLogger>();

			_callbackActivatorMock.Setup(cam => cam.SerializeCallback(It.IsAny<Action<Stream, object>>(), It.IsAny<object>()))
				.Returns(new Callback("", "", "", ""));

			_feedProcessor = new FeedProcessor(_amazonRegion, _merchantId, options, _marketplaceWebServiceClientMock.Object,
				_feedSubmissionProcessorMock.Object, _callbackActivatorMock.Object, _loggerMock.Object);
		}

		#region QueueFeed tests 

		[Test]
		public void QueueFeed_WithNullCallbackMethodArgument_NeverCallsLogError()
		{
			var propertiesContainer = new FeedSubmissionPropertiesContainer("testFeedContent", "testFeedType");
			var callbackMethod = (Action<Stream, object>) null;

			_feedProcessor.QueueFeed(_feedSubmissionServiceMock.Object, propertiesContainer, callbackMethod, new { Foo = "Bar" });

			_feedSubmissionServiceMock.Verify(rrcs => rrcs.Create(It.IsAny<FeedSubmissionEntry>()), Times.Never);
			_feedSubmissionServiceMock.Verify(rrcs => rrcs.SaveChanges(), Times.Never);
			_loggerMock.Verify(lm => lm.Error(It.IsAny<string>(), It.IsAny<ArgumentNullException>()), Times.Once);
		}

		[Test]
		public void QueueFeed_WithNullReportRequestPropertiesContainerArgument_ThrowsArgumentNullException()
		{
			FeedSubmissionPropertiesContainer propertiesContainer = null;
			var callbackMethod = new Action<Stream, object>((stream, o) => { _called = true; });

			_feedProcessor.QueueFeed(_feedSubmissionServiceMock.Object, propertiesContainer, callbackMethod, new { Foo = "Bar" });

			_loggerMock.Verify(lm => lm.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
		}

		[Test]
		public void QueueFeed_WithNonEmptyArguments_CallsFeedSubmissionEntryServiceCreateOnceWithCorrectData()
		{
			var propertiesContainer = new FeedSubmissionPropertiesContainer("testFeedContent", "testFeedType");
			var callbackMethod = new Action<Stream, object>((stream, o) => { _called = true; });
			FeedSubmissionEntry feedSubmissionEntry = null;
			_feedSubmissionServiceMock.Setup(rrcsm => rrcsm.Create(It.IsAny<FeedSubmissionEntry>()))
				.Callback<FeedSubmissionEntry>((p) => { feedSubmissionEntry = p; });

			_feedProcessor.QueueFeed(_feedSubmissionServiceMock.Object, propertiesContainer, callbackMethod, new CallbackActivatorTests.CallbackDataTest {Foo = "Bar"});

			_feedSubmissionServiceMock.Verify(rrcsm => rrcsm.Create(It.IsAny<FeedSubmissionEntry>()), Times.Once);
			Assert.AreEqual(JsonConvert.SerializeObject(propertiesContainer), feedSubmissionEntry.FeedSubmissionData);
			Assert.AreEqual(AmazonRegion.Europe, feedSubmissionEntry.AmazonRegion);
			Assert.NotNull(feedSubmissionEntry.TypeName);
			Assert.NotNull(feedSubmissionEntry.Data);
			Assert.NotNull(feedSubmissionEntry.DataTypeName);
			Assert.NotNull(feedSubmissionEntry.MethodName);
		}

		[Test]
		public void QueueFeed_WithNonEmptyArguments_CallsFeedSubmissionEntryServiceSaveChangesOnce()
		{
			var propertiesContainer = new FeedSubmissionPropertiesContainer("testFeedContent", "testFeedType");
			var callbackMethod = new Action<Stream, object>((stream, o) => { _called = true; });

			_feedProcessor.QueueFeed(_feedSubmissionServiceMock.Object, propertiesContainer, callbackMethod, new CallbackActivatorTests.CallbackDataTest {Foo = "Bar"});

			_feedSubmissionServiceMock.Verify(rrcsm => rrcsm.SaveChanges(), Times.Once);
		}

		#endregion


		#region PollFeeds tests 

		[Test]
		public void Poll_CallsOnce_GetNextFromQueueOfFeedsToSubmit()
		{
			_feedProcessor.PollFeeds(_feedSubmissionServiceMock.Object);

			_feedSubmissionServiceMock.Verify(
				rrp => rrp.GetNextFromQueueOfFeedsToSubmit(It.IsAny<string>(), It.IsAny<AmazonRegion>(), MarkEntryAsHandled), Times.Once);
		}

		[Test]
		public void Poll_WithGetNextFeedToSubmitFromQueueReturningNull_DoesNotSubmitFeedToAmazon()
		{
			_feedSubmissionServiceMock
				.Setup(rrp => rrp.GetNextFromQueueOfFeedsToSubmit(It.IsAny<string>(), It.IsAny<AmazonRegion>(), MarkEntryAsHandled))
				.Returns((FeedSubmissionEntry) null);

			_feedProcessor.PollFeeds(_feedSubmissionServiceMock.Object);

			_feedSubmissionProcessorMock.Verify(
				rrp => rrp.SubmitFeedToAmazon(It.IsAny<IFeedSubmissionEntryService>(),It.IsAny<FeedSubmissionEntry>()), Times.Never);
		}

		[Test]
		public void Poll_WithGetNextFeedToSubmitFromQueueReturningNotNull_DoesSubmitFeedToAmazon()
		{
			var propertiesContainer = new FeedSubmissionPropertiesContainer("testFeedContent", "testFeedType");
			var serializedPropertiesContainer = JsonConvert.SerializeObject(propertiesContainer);

			_feedSubmissionServiceMock
				.Setup(rrp => rrp.GetNextFromQueueOfFeedsToSubmit(It.IsAny<string>(), It.IsAny<AmazonRegion>(), MarkEntryAsHandled))
				.Returns(new FeedSubmissionEntry(serializedPropertiesContainer));

			_feedProcessor.PollFeeds(_feedSubmissionServiceMock.Object);

			_feedSubmissionProcessorMock.Verify(
				rrp => rrp.SubmitFeedToAmazon(It.IsAny<IFeedSubmissionEntryService>(),It.IsAny<FeedSubmissionEntry>()), Times.Once);
		}

		#endregion
	}
}