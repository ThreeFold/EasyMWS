﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MarketplaceWebService;
using MarketplaceWebService.Model;
using Moq;
using MountainWarehouse.EasyMWS;
using MountainWarehouse.EasyMWS.Data;
using MountainWarehouse.EasyMWS.Factories.Reports;
using MountainWarehouse.EasyMWS.Helpers;
using MountainWarehouse.EasyMWS.ReportProcessors;
using MountainWarehouse.EasyMWS.Services;
using NUnit.Framework;

namespace EasyMWS.Tests.ReportProcessors
{
	[TestFixture]
	public class RequestReportProcessorTests
	{
		private AmazonRegion _region = AmazonRegion.Europe;
		private EasyMwsClient _easyMwsClient = new EasyMwsClient(AmazonRegion.Europe, "MerchantId", "AccessKeyTest", "SecretAccessKeyTest");
		private ReportRequestFactoryFba _reportRequestFactoryFba;
		private IRequestReportProcessor _requestReportProcessor;
		private Mock<IReportRequestCallbackService> _reportRequestCallbackServiceMock;
		private List<ReportRequestCallback> _reportRequestCallbacks;
		private Mock<IMarketplaceWebServiceClient> _marketplaceWebServiceClientMock;
		private EasyMwsOptions _easyMwsOptions;

		[SetUp]
		public void SetUp()
		{
			_easyMwsOptions = EasyMwsOptions.Defaults;

			_marketplaceWebServiceClientMock = new Mock<IMarketplaceWebServiceClient>();
			_reportRequestFactoryFba = new ReportRequestFactoryFba();
			_reportRequestCallbackServiceMock = new Mock<IReportRequestCallbackService>();
			_requestReportProcessor = new RequestReportProcessor(_marketplaceWebServiceClientMock.Object, _reportRequestCallbackServiceMock.Object, _easyMwsOptions);
			
			_reportRequestCallbacks = new List<ReportRequestCallback>
			{
				new ReportRequestCallback
				{
					AmazonRegion = AmazonRegion.China,
					Id = 1,
					RequestReportId = null,
					GeneratedReportId = null,
					ReportRequestData = "{\"UpdateFrequency\":0,\"ReportType\":\"_GET_AFN_INVENTORY_DATA_\",\"Merchant\":null,\"MwsAuthToken\":null,\"Region\":20,\"MarketplaceIdList\":null}"
				}
			};

			var requestReportRequest = new RequestReportResponse
			{
				RequestReportResult = new RequestReportResult
				{
					ReportRequestInfo = new ReportRequestInfo
					{
						ReportRequestId = "Report001"
					}
				}
			};

			var getReportRequestListResponse = new GetReportRequestListResponse
			{
				GetReportRequestListResult = new GetReportRequestListResult
				{
					ReportRequestInfo = new List<ReportRequestInfo>
					{
						new ReportRequestInfo
						{
							ReportProcessingStatus = "_DONE_",
							ReportRequestId = "Report1",
							GeneratedReportId = "testGeneratedReportId"
						},
						new ReportRequestInfo
						{
							ReportProcessingStatus = "_CANCELLED_",
							ReportRequestId = "Report2",
							GeneratedReportId = null
						},
						new ReportRequestInfo
						{
							ReportProcessingStatus = "_OTHER_",
							ReportRequestId = "Report3",
							GeneratedReportId = null
						}
					}
				}
			};

			var reportRequestCallbacks = _reportRequestCallbacks.AsQueryable();

			_reportRequestCallbackServiceMock.Setup(x => x.Where(It.IsAny<Expression<Func<ReportRequestCallback, bool>>>()))
				.Returns((Expression<Func<ReportRequestCallback, bool>> e) => reportRequestCallbacks.Where(e));

			_reportRequestCallbackServiceMock.Setup(x => x.GetAll()).Returns(reportRequestCallbacks);

			_marketplaceWebServiceClientMock.Setup(x => x.RequestReport(It.IsAny<RequestReportRequest>()))
				.Returns(requestReportRequest);

			_marketplaceWebServiceClientMock.Setup(x => x.GetReportRequestList(It.IsAny<GetReportRequestListRequest>()))
				.Returns(getReportRequestListResponse);

			_reportRequestCallbackServiceMock
				.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<ReportRequestCallback, bool>>>()))
				.Returns((Expression<Func<ReportRequestCallback, bool>> e) => reportRequestCallbacks.FirstOrDefault(e));

			_marketplaceWebServiceClientMock.Setup(x => x.GetReport(It.IsAny<GetReportRequest>()))
				.Returns(new GetReportResponse());
		}

		[Test]
		public void GetNonRequestedReportsFromQueue_ReturnsFirstReportRequestFromQueueForGivenRegion_AndSkipsReportRequestsForDifferentRegions()
		{
			var testRegion = AmazonRegion.Europe;
			var reportRequestWithDifferentRegion = new ReportRequestCallback { AmazonRegion = AmazonRegion.Australia, Id = 2, RequestReportId = null, RequestRetryCount = 0};
			var reportRequestWithCorrectRegion1 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 3, RequestReportId = null, RequestRetryCount = 0 };
			var reportRequestWithCorrectRegion2 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 4, RequestReportId = null, RequestRetryCount = 0 };


			_reportRequestCallbacks.Add(reportRequestWithDifferentRegion);
			_reportRequestCallbacks.Add(reportRequestWithCorrectRegion1);
			_reportRequestCallbacks.Add(reportRequestWithCorrectRegion2);

			var reportRequestCallback =
				_requestReportProcessor.GetNonRequestedReportFromQueue(testRegion);

			Assert.AreEqual(reportRequestWithCorrectRegion1.Id, reportRequestCallback.Id);
		}

		[Test]
		public void GetNonRequestedReportsFromQueue_ReturnsFirstReportRequestFromQueueWithNullRequestReportId_AndSkipsReportRequestsWithNonNullRequestReportId()
		{
			var testRegion = AmazonRegion.Europe;
			var reportRequestWithDifferentRegion = new ReportRequestCallback { AmazonRegion = AmazonRegion.Australia, Id = 2, RequestReportId = null, RequestRetryCount = 0 };
			var reportRequestWithNonNullRequestReportId = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 3, RequestReportId = "testRequestReportId", RequestRetryCount = 0 };
			var reportRequestWithNonNullRequestReportId1 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 4, RequestReportId = null, RequestRetryCount = 0 };
			var reportRequestWithNonNullRequestReportId2 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 5, RequestReportId = null, RequestRetryCount = 0 };


			_reportRequestCallbacks.Add(reportRequestWithDifferentRegion);
			_reportRequestCallbacks.Add(reportRequestWithNonNullRequestReportId);
			_reportRequestCallbacks.Add(reportRequestWithNonNullRequestReportId1);
			_reportRequestCallbacks.Add(reportRequestWithNonNullRequestReportId2);

			var reportRequestCallback =
				_requestReportProcessor.GetNonRequestedReportFromQueue(testRegion);

			Assert.AreEqual(reportRequestWithNonNullRequestReportId1.Id, reportRequestCallback.Id);
		}

		[Test]
		public void GetNonRequestedReportsFromQueue_ReturnsFirstReportRequestFromQueueWithNoRequestRetryCount_AndSkipsReportRequestsWithRequestRetryPeriodIncomplete()
		{
			var testRegion = AmazonRegion.Europe;
			var reportRequestWithRequestRetryPeriodIncomplete = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 2, RequestReportId = null, RequestRetryCount = 1, LastRequested = DateTime.UtcNow.AddHours(-1)};
			_easyMwsOptions.TimeToWaitBeforeFirstRetry = TimeSpan.FromHours(2);
			_easyMwsOptions.TimeToWaitBetweenRetries = TimeSpan.FromHours(2);
			_requestReportProcessor = new RequestReportProcessor(_marketplaceWebServiceClientMock.Object, _reportRequestCallbackServiceMock.Object, _easyMwsOptions);
			var reportRequestWithNoRequestRetryCount1 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 3, RequestReportId = null, RequestRetryCount = 0, LastRequested = DateTime.MinValue };
			var reportRequestWithNoRequestRetryCount2 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 4, RequestReportId = null, RequestRetryCount = 0, LastRequested = DateTime.MinValue };

			_reportRequestCallbacks.Add(reportRequestWithRequestRetryPeriodIncomplete);
			_reportRequestCallbacks.Add(reportRequestWithNoRequestRetryCount1);
			_reportRequestCallbacks.Add(reportRequestWithNoRequestRetryCount2);

			var reportRequestCallback =
				_requestReportProcessor.GetNonRequestedReportFromQueue(testRegion);

			Assert.AreEqual(reportRequestWithNoRequestRetryCount1.Id, reportRequestCallback.Id);
		}

		[Test]
		public void GetNonRequestedReportsFromQueue_ReturnsFirstReportRequestFromQueueWithCompleteRetryPeriod_AndSkipsReportRequestsWithRequestRetryPeriodIncomplete()
		{
			var testRegion = AmazonRegion.Europe;
			var reportRequestWithRequestRetryPeriodIncomplete = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 2, RequestReportId = null, RequestRetryCount = 1, LastRequested = DateTime.UtcNow.AddMinutes(-30) };
			_easyMwsOptions.TimeToWaitBeforeFirstRetry = TimeSpan.FromHours(1);
			_easyMwsOptions.TimeToWaitBetweenRetries = TimeSpan.FromHours(1);
			_requestReportProcessor = new RequestReportProcessor(_marketplaceWebServiceClientMock.Object, _reportRequestCallbackServiceMock.Object, _easyMwsOptions);
			var reportRequestWithNoRetryPeriodComplete1 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 3, RequestReportId = null, RequestRetryCount = 0, LastRequested = DateTime.UtcNow.AddMinutes(-61) };
			var reportRequestWithNoRetryPeriodComplete2 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 4, RequestReportId = null, RequestRetryCount = 0, LastRequested = DateTime.UtcNow.AddMinutes(-61) };

			_reportRequestCallbacks.Add(reportRequestWithRequestRetryPeriodIncomplete);
			_reportRequestCallbacks.Add(reportRequestWithNoRetryPeriodComplete1);
			_reportRequestCallbacks.Add(reportRequestWithNoRetryPeriodComplete2);

			var reportRequestCallback =
				_requestReportProcessor.GetNonRequestedReportFromQueue(testRegion);

			Assert.AreEqual(reportRequestWithNoRetryPeriodComplete1.Id, reportRequestCallback.Id);
		}

		[Test]
		public void GetNonRequestedReportsFromQueue_WithConfiguredTimeToWaitBeforeFirstRetry_AndInitialRetryCount_ReturnsReportRequestWithTheExpectedCompleteRetryPeriod()
		{
			var reportRequestWithRequestRetryPeriodIncomplete = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 2, RequestReportId = null, RequestRetryCount = 1, LastRequested = DateTime.UtcNow.AddMinutes(-59) };
			_easyMwsOptions.TimeToWaitBeforeFirstRetry = TimeSpan.FromMinutes(60);
			_easyMwsOptions.TimeToWaitBetweenRetries = TimeSpan.FromMinutes(1);
			_requestReportProcessor = new RequestReportProcessor(_marketplaceWebServiceClientMock.Object, _reportRequestCallbackServiceMock.Object, _easyMwsOptions);
			var reportRequestWithNoRetryPeriodComplete1 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 3, RequestReportId = null, RequestRetryCount = 1, LastRequested = DateTime.UtcNow.AddMinutes(-61) };
			var reportRequestWithNoRetryPeriodComplete2 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 4, RequestReportId = null, RequestRetryCount = 1, LastRequested = DateTime.UtcNow.AddMinutes(-61) };

			_reportRequestCallbacks.Add(reportRequestWithRequestRetryPeriodIncomplete);
			_reportRequestCallbacks.Add(reportRequestWithNoRetryPeriodComplete1);
			_reportRequestCallbacks.Add(reportRequestWithNoRetryPeriodComplete2);

			var reportRequestCallback = _requestReportProcessor.GetNonRequestedReportFromQueue(AmazonRegion.Europe);

			Assert.AreEqual(reportRequestWithNoRetryPeriodComplete1.Id, reportRequestCallback.Id);
		}

		[Test]
		public void GetNonRequestedReportsFromQueue_WithRetryPeriodTypeConfiguredAsArithmeticProgression_AndNonInitialRetryCount_ReturnsReportRequestWithTheExpectedCompleteRetryPeriod()
		{
			var reportRequestWithRequestRetryPeriodIncomplete = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 2, RequestReportId = null, RequestRetryCount = 5, LastRequested = DateTime.UtcNow.AddMinutes(-59) };
			_easyMwsOptions.TimeToWaitBeforeFirstRetry = TimeSpan.FromMinutes(1);
			_easyMwsOptions.TimeToWaitBetweenRetries = TimeSpan.FromMinutes(60);
			_easyMwsOptions.MaxRequestRetryCount = 10;
			_easyMwsOptions.RetryPeriodType = RetryPeriodType.ArithmeticProgression;
			_requestReportProcessor = new RequestReportProcessor(_marketplaceWebServiceClientMock.Object, _reportRequestCallbackServiceMock.Object, _easyMwsOptions);
			var reportRequestWithNoRetryPeriodComplete1 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 3, RequestReportId = null, RequestRetryCount = 5, LastRequested = DateTime.UtcNow.AddMinutes(-61) };
			var reportRequestWithNoRetryPeriodComplete2 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 4, RequestReportId = null, RequestRetryCount = 5, LastRequested = DateTime.UtcNow.AddMinutes(-61) };

			_reportRequestCallbacks.Add(reportRequestWithRequestRetryPeriodIncomplete);
			_reportRequestCallbacks.Add(reportRequestWithNoRetryPeriodComplete1);
			_reportRequestCallbacks.Add(reportRequestWithNoRetryPeriodComplete2);

			var reportRequestCallback = _requestReportProcessor.GetNonRequestedReportFromQueue(AmazonRegion.Europe);

			Assert.AreEqual(reportRequestWithNoRetryPeriodComplete1.Id, reportRequestCallback.Id);
		}

		[Test]
		public void GetNonRequestedReportsFromQueue_WithRetryPeriodTypeConfiguredAsGeometricProgression_AndNonInitialRetryCount_ReturnsReportRequestWithTheExpectedCompleteRetryPeriod()
		{
			var testRequestRetryCount = 5;
			var minutesBetweenRetries = 60;
			var reportRequestWithRequestRetryPeriodIncomplete = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 2, RequestReportId = null,
				RequestRetryCount = testRequestRetryCount, LastRequested = DateTime.UtcNow.AddMinutes(-61) };
			_easyMwsOptions.TimeToWaitBeforeFirstRetry = TimeSpan.FromMinutes(1);
			_easyMwsOptions.TimeToWaitBetweenRetries = TimeSpan.FromMinutes(minutesBetweenRetries);
			_easyMwsOptions.MaxRequestRetryCount = 10;
			_easyMwsOptions.RetryPeriodType = RetryPeriodType.GeometricProgression;
			_requestReportProcessor = new RequestReportProcessor(_marketplaceWebServiceClientMock.Object, _reportRequestCallbackServiceMock.Object, _easyMwsOptions);
			var reportRequestWithNoRetryPeriodComplete1 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 3, RequestReportId = null,
				RequestRetryCount = testRequestRetryCount, LastRequested = DateTime.UtcNow.AddMinutes(-59) };
			var reportRequestWithNoRetryPeriodComplete2 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 4, RequestReportId = null,
				RequestRetryCount = testRequestRetryCount, LastRequested = DateTime.UtcNow.AddMinutes(-(testRequestRetryCount * minutesBetweenRetries - 1)) };
			var reportRequestWithNoRetryPeriodComplete3 = new ReportRequestCallback { AmazonRegion = AmazonRegion.Europe, Id = 5, RequestReportId = null,
				RequestRetryCount = testRequestRetryCount, LastRequested = DateTime.UtcNow.AddMinutes(-(testRequestRetryCount * minutesBetweenRetries - 1)) };

			_reportRequestCallbacks.Add(reportRequestWithRequestRetryPeriodIncomplete);
			_reportRequestCallbacks.Add(reportRequestWithNoRetryPeriodComplete1);
			_reportRequestCallbacks.Add(reportRequestWithNoRetryPeriodComplete2);
			_reportRequestCallbacks.Add(reportRequestWithNoRetryPeriodComplete3);

			var reportRequestCallback = _requestReportProcessor.GetNonRequestedReportFromQueue(AmazonRegion.Europe);

			Assert.AreEqual(reportRequestWithNoRetryPeriodComplete2.Id, reportRequestCallback.Id);
		}

		[Test]
		public void RequestSingleQueuedReport_OneInQueue_SubmitsToAmazon()
		{
			var reportId = _requestReportProcessor.RequestSingleQueuedReport(_reportRequestCallbacks[0], "");

			_marketplaceWebServiceClientMock.Verify(mwsc => mwsc.RequestReport(It.IsAny<RequestReportRequest>()), Times.Once);
			Assert.AreEqual("Report001", reportId);
		}

		[Test]
		public void MoveToNonGeneratedReportsQueue_UpdatesRequestReportId_OnTheCallback()
		{
			var reportRequestId = "testReportRequestId";

			 _requestReportProcessor.MoveToNonGeneratedReportsQueue(_reportRequestCallbacks[0], reportRequestId);

			Assert.AreEqual("testReportRequestId", _reportRequestCallbacks[0].RequestReportId);
			_reportRequestCallbackServiceMock.Verify(x => x.Update(It.IsAny<ReportRequestCallback>()), Times.Once);
		}

		[Test]
		public void GetAllPendingReport_ReturnListReportRequestId()
		{
			// Arrange
			var data = new List<ReportRequestCallback>
			{
				new ReportRequestCallback
				{
					AmazonRegion = AmazonRegion.Europe,
					Id = 2,
					RequestReportId = "Report1",
					GeneratedReportId = null
				},
				new ReportRequestCallback
				{
					AmazonRegion = AmazonRegion.Europe,
					Id = 3,
					RequestReportId = "Report2",
					GeneratedReportId = null
				},
				new ReportRequestCallback
				{
					AmazonRegion = AmazonRegion.Europe,
					Id = 4,
					RequestReportId = "Report3",
					GeneratedReportId = "GeneratedIdTest1"
				},
				new ReportRequestCallback
				{
					AmazonRegion = AmazonRegion.NorthAmerica,
					Id = 5,
					RequestReportId = "Report4",
					GeneratedReportId = null
				}
			};
			
			_reportRequestCallbacks.AddRange(data);

			// Act
			var listPendingReports = _requestReportProcessor.GetAllPendingReport(_region);
			
			// Assert
			Assert.AreEqual(2, listPendingReports.Count());
		}

		[Test]
		public void RequestReportsStatuses_WithMultiplePendingReports_SubmitsAmazonRequest()
		{
			var testRequestIdList = new List<string>{ "Report1", "Report2", "Report3" };

			var result = _requestReportProcessor.GetReportRequestListResponse(testRequestIdList, "");

			Assert.AreEqual("testGeneratedReportId", result.First(x => x.ReportRequestId == "Report1").GeneratedReportId);
			Assert.AreEqual("_DONE_", result.First(x => x.ReportRequestId == "Report1").ReportProcessingStatus);
			Assert.AreEqual("_CANCELLED_", result.First(x => x.ReportRequestId == "Report2").ReportProcessingStatus);
			Assert.IsNull(result.First(x => x.ReportRequestId == "Report2").GeneratedReportId);
		}

		[Test]
		public void MoveReportsToGeneratedQueue_UpdateGeneratedRequestId()
		{
			// Arrange
			var data = new List<ReportRequestCallback>
			{
				new ReportRequestCallback
				{
					AmazonRegion = AmazonRegion.Europe,
					Id = 2,
					RequestReportId = "Report1",
					GeneratedReportId = null
				},
				new ReportRequestCallback
				{
					AmazonRegion = AmazonRegion.Europe,
					Id = 3,
					RequestReportId = "Report2",
					GeneratedReportId = null
				},
				new ReportRequestCallback
				{
					AmazonRegion = AmazonRegion.Europe,
					Id = 4,
					RequestReportId = "Report3",
					GeneratedReportId = "GeneratedIdTest1"
				},
				new ReportRequestCallback
				{
					AmazonRegion = AmazonRegion.NorthAmerica,
					Id = 5,
					RequestReportId = "Report4",
					GeneratedReportId = null
				}
			};

			_reportRequestCallbacks.AddRange(data);

			var dataResult = new List<(string ReportRequestId, string GeneratedReportId, string ReportProcessingStatus)>
			{
				("Report1", "GeneratedId1", "_DONE_"),
				("Report2", "GeneratedId2", "_DONE_"),
				("Report3", null, "_CANCELLED_"),
				("Report4", null, "_OTHER_")
			};

			_requestReportProcessor.MoveReportsToGeneratedQueue(dataResult);

			Assert.AreEqual("GeneratedId1", _reportRequestCallbacks.First(x => x.RequestReportId == "Report1").GeneratedReportId);
			_reportRequestCallbackServiceMock.Verify(x => x.Update(It.IsAny<ReportRequestCallback>()), Times.Exactly(2));
		}

		[Test]
		public void MoveReportsBackToRequestQueue_UpdateReportRequestId()
		{
			_reportRequestCallbacks.First().RequestReportId = "Report3";

			var data = new List<(string ReportRequestId, string GeneratedReportId, string ReportProcessingStatus)>
			{
				("Report1", "GeneratedId1", "_DONE_"),
				("Report2", "GeneratedId2", "_DONE_"),
				("Report3", null, "_CANCELLED_"),
				("Report4", null, "_OTHER_")
			};

			_requestReportProcessor.MoveReportsBackToRequestQueue(data);

			Assert.IsNull(_reportRequestCallbacks.First().RequestReportId);
			Assert.IsTrue(_reportRequestCallbacks.First().RequestRetryCount > 0);
			_reportRequestCallbackServiceMock.Verify(x => x.Update(It.IsAny<ReportRequestCallback>()), Times.Once);
		}

		[Test]
		public void GetReadyForDownloadReports_ReturnListOfReports_GeneratedIdNotNull()
		{
			// Arrange
			var data = new List<ReportRequestCallback>
			{
				new ReportRequestCallback
				{
					AmazonRegion = AmazonRegion.Europe,
					Id = 2,
					RequestReportId = "Report1",
					GeneratedReportId = null
				},
				new ReportRequestCallback
				{
					AmazonRegion = AmazonRegion.Europe,
					Id = 3,
					RequestReportId = "Report2",
					GeneratedReportId = "GeneratedIdTest2"
				},
				new ReportRequestCallback
				{
					AmazonRegion = AmazonRegion.Europe,
					Id = 4,
					RequestReportId = "Report3",
					GeneratedReportId = "GeneratedIdTest3"
				},
				new ReportRequestCallback
				{
					AmazonRegion = AmazonRegion.NorthAmerica,
					Id = 5,
					RequestReportId = "Report4",
					GeneratedReportId = null
				}
			};
			_reportRequestCallbacks.AddRange(data);

			var result = _requestReportProcessor.GetReadyForDownloadReports(_region);

			Assert.AreEqual(3, result.Id);
		}

		[Test]
		public void DownloadGeneratedReport_ShouldDownloadReportFromAmazon_ReturnStream()
		{
			// Arrange
			var merchantId = "testMerchantId";

			var reportRequestCallback = new ReportRequestCallback
			{
				Data = null,
				AmazonRegion = AmazonRegion.Europe,
				Id = 4,
				RequestReportId = "Report3",
				GeneratedReportId = "GeneratedIdTest1"
			};

			_reportRequestCallbacks.Add(reportRequestCallback);

			// Act
			var testData = _reportRequestCallbacks.Find(x => x.GeneratedReportId == "GeneratedIdTest1");
			var result = _requestReportProcessor.DownloadGeneratedReport(testData, merchantId);

			// Assert
			_marketplaceWebServiceClientMock.Verify(x => x.GetReport(It.IsAny<GetReportRequest>()), Times.Once);
			Assert.IsNotNull(result);
		}

		[Test]
		public void AllocateReportRequestForRetry_CalledOnce_IncrementsRequestRetryCountCorrectly()
		{
			Assert.AreEqual(0, _reportRequestCallbacks.First().RequestRetryCount);

			_requestReportProcessor.AllocateReportRequestForRetry(_reportRequestCallbacks.First());

			Assert.AreEqual(1, _reportRequestCallbacks.First().RequestRetryCount);
			_reportRequestCallbackServiceMock.Verify(x => x.Update(It.IsAny<ReportRequestCallback>()), Times.Once);
		}

		[Test]
		public void AllocateReportRequestForRetry_CalledMultipleTimes_IncrementsRequestRetryCountCorrectly()
		{
			Assert.AreEqual(0, _reportRequestCallbacks.First().RequestRetryCount);

			_requestReportProcessor.AllocateReportRequestForRetry(_reportRequestCallbacks.First());
			_requestReportProcessor.AllocateReportRequestForRetry(_reportRequestCallbacks.First());
			_requestReportProcessor.AllocateReportRequestForRetry(_reportRequestCallbacks.First());

			Assert.AreEqual(3, _reportRequestCallbacks.First().RequestRetryCount);
			_reportRequestCallbackServiceMock.Verify(x => x.Update(It.IsAny<ReportRequestCallback>()), Times.Exactly(3));
		}
	}
}