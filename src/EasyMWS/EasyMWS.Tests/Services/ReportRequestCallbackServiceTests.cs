﻿
using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using MountainWarehouse.EasyMWS;
using MountainWarehouse.EasyMWS.Data;
using MountainWarehouse.EasyMWS.Enums;
using MountainWarehouse.EasyMWS.Helpers;
using MountainWarehouse.EasyMWS.Model;
using MountainWarehouse.EasyMWS.Processors;
using MountainWarehouse.EasyMWS.Repositories;
using MountainWarehouse.EasyMWS.Services;
using Newtonsoft.Json;
using NUnit.Framework;

namespace EasyMWS.Tests.Services
{
	[TestFixture]
    public class ReportRequestCallbackServiceTests
    {
	    private Mock<IReportRequestEntryRepository> _reportRequestCallbackReportMock;
	    private ReportRequestEntryService _reportRequestEntryService;
	    private readonly string _merchantId = "TestMerchantId";
	    private readonly AmazonRegion _amazonRegion = AmazonRegion.Europe;
	    private List<ReportRequestEntry> _reportRequestEntries;
	    private readonly EasyMwsOptions _options = EasyMwsOptions.Defaults();

		[SetUp]
	    public void SetUp()
	    {
		    var reportRequestPropertiesContainer = new ReportRequestPropertiesContainer("_Report_Type_", ContentUpdateFrequency.NearRealTime, new List<string>(MwsMarketplaceGroup.AmazonEurope()));

		    _reportRequestEntries = new List<ReportRequestEntry>
		    {
			    new ReportRequestEntry
				{
					AmazonRegion = AmazonRegion.Europe,
					Data = "testData",
					ReportRequestData = JsonConvert.SerializeObject(reportRequestPropertiesContainer),
					MethodName = "testMethodName",
					TypeName = "testTypeName",
					LastAmazonRequestDate = DateTime.MinValue,
					DataTypeName = "testDataTypeName",
					ContentUpdateFrequency = 0,
					Id = 1,
					ReportType = reportRequestPropertiesContainer.ReportType
				},
				new ReportRequestEntry{Id = 2}
		    };

			_reportRequestCallbackReportMock = new Mock<IReportRequestEntryRepository>();
		    _reportRequestCallbackReportMock.Setup(x => x.GetAll()).Returns(_reportRequestEntries.AsQueryable());
			_reportRequestEntryService = new ReportRequestEntryService(_reportRequestCallbackReportMock.Object);
	    }


		[Test]
		public void FirstOrDefault_TwoInQueue_ReturnsFirstObjectContainingCorrectData()
		{
			var reportRequestCallback = _reportRequestEntryService.FirstOrDefault();
			var reportRequestData = JsonConvert.DeserializeObject<ReportRequestPropertiesContainer>(reportRequestCallback.ReportRequestData);

			Assert.AreEqual(AmazonRegion.Europe, reportRequestCallback.AmazonRegion);
			Assert.AreEqual("testData", reportRequestCallback.Data);
			Assert.AreEqual("testMethodName", reportRequestCallback.MethodName);
			Assert.AreEqual("testTypeName", reportRequestCallback.TypeName);
			Assert.AreEqual("testDataTypeName", reportRequestCallback.DataTypeName);
			Assert.AreEqual("_Report_Type_", reportRequestCallback.ReportType);
			Assert.AreEqual(ContentUpdateFrequency.NearRealTime, reportRequestData.UpdateFrequency);
			CollectionAssert.AreEquivalent(new List<string>(MwsMarketplaceGroup.AmazonEurope()), reportRequestData.MarketplaceIdList);
		}

	    [Test]
	    public void GetNextFromQueueOfReportsToDownload_ReturnListOfReports_GeneratedIdNotNull_ForGivenRegionAndMerchantId()
	    {
		    // Arrange
		    var merchantId2 = "test merchant id 2";
		    var data = new List<ReportRequestEntry>
		    {
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId,
				    Id = 2,
				    RequestReportId = "Report1",
				    GeneratedReportId = null
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = merchantId2,
				    Id = 3,
				    RequestReportId = "Report2",
				    GeneratedReportId = "GeneratedIdTest2"
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId,
				    Id = 4,
				    RequestReportId = "Report4",
				    GeneratedReportId = "GeneratedIdTest2"
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId,
				    Id = 5,
				    RequestReportId = "Report5",
				    GeneratedReportId = "GeneratedIdTest3"
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.NorthAmerica, MerchantId = _merchantId,
				    Id = 6,
				    RequestReportId = "Report6",
				    GeneratedReportId = null
			    }
		    };
		    _reportRequestEntries.AddRange(data);

		    var result = _reportRequestEntryService.GetNextFromQueueOfReportsToDownload(_options, _merchantId, _amazonRegion);

		    Assert.AreEqual(4, result?.Id);
	    }

	    [Test]
	    public void GetNextFromQueueOfReportsToRequest_ReturnsFirstReportRequestFromQueueForGivenMerchant_AndSkipsReportRequestsForDifferentMerchants()
	    {
		    var testMerchantId2 = "testMerchantId2";
		    var reportRequestWithDifferentMerchant = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = testMerchantId2, Id = 2, RequestReportId = null, ReportRequestRetryCount = 0 };
		    var reportRequestWithCorrectRegion1 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 3, RequestReportId = null, ReportRequestRetryCount = 0 };
		    var reportRequestWithCorrectRegion2 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 4, RequestReportId = null, ReportRequestRetryCount = 0 };


		    _reportRequestEntries.Add(reportRequestWithDifferentMerchant);
		    _reportRequestEntries.Add(reportRequestWithCorrectRegion1);
		    _reportRequestEntries.Add(reportRequestWithCorrectRegion2);

		    var reportRequestCallback =
			    _reportRequestEntryService.GetNextFromQueueOfReportsToRequest(_options, _merchantId, _amazonRegion);

		    Assert.AreEqual(reportRequestWithCorrectRegion1.Id, reportRequestCallback.Id);
	    }

		[Test]
		public void GetNextFromQueueOfReportsToRequest_ReturnsFirstReportRequestFromQueueForGivenRegion_AndSkipsReportRequestsForDifferentRegions()
		{
			var reportRequestWithDifferentRegion = new ReportRequestEntry { AmazonRegion = AmazonRegion.Australia, MerchantId = _merchantId, Id = 2, RequestReportId = null, ReportRequestRetryCount = 0 };
			var reportRequestWithCorrectRegion1 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 3, RequestReportId = null, ReportRequestRetryCount = 0 };
			var reportRequestWithCorrectRegion2 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 4, RequestReportId = null, ReportRequestRetryCount = 0 };


			_reportRequestEntries.Add(reportRequestWithDifferentRegion);
			_reportRequestEntries.Add(reportRequestWithCorrectRegion1);
			_reportRequestEntries.Add(reportRequestWithCorrectRegion2);

			var reportRequestCallback =
				_reportRequestEntryService.GetNextFromQueueOfReportsToRequest(_options, _merchantId, _amazonRegion);

			Assert.AreEqual(reportRequestWithCorrectRegion1.Id, reportRequestCallback.Id);
		}

		[Test]
		public void GetNextFromQueueOfReportsToRequest_ReturnsFirstReportRequestFromQueueWithNullRequestReportId_AndSkipsReportRequestsWithNonNullRequestReportId()
		{
			var reportRequestWithDifferentRegion = new ReportRequestEntry { AmazonRegion = AmazonRegion.Australia, MerchantId = _merchantId, Id = 2, RequestReportId = null, ReportRequestRetryCount = 0 };
			var reportRequestWithNonNullRequestReportId = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 3, RequestReportId = "testRequestReportId", ReportRequestRetryCount = 0 };
			var reportRequestWithNullRequestReportId1 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 4, RequestReportId = null, ReportRequestRetryCount = 0 };
			var reportRequestWithNullRequestReportId2 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 5, RequestReportId = null, ReportRequestRetryCount = 0 };


			_reportRequestEntries.Add(reportRequestWithDifferentRegion);
			_reportRequestEntries.Add(reportRequestWithNonNullRequestReportId);
			_reportRequestEntries.Add(reportRequestWithNullRequestReportId1);
			_reportRequestEntries.Add(reportRequestWithNullRequestReportId2);

			var reportRequestCallback =
				_reportRequestEntryService.GetNextFromQueueOfReportsToRequest(_options, _merchantId, _amazonRegion);

			Assert.AreEqual(reportRequestWithNullRequestReportId1.Id, reportRequestCallback.Id);
		}

		[Test]
		public void GetNextFromQueueOfReportsToRequest_ReturnsFirstReportRequestFromQueueWithNoRequestRetryCount_AndSkipsReportRequestsWithRequestRetryPeriodIncomplete()
		{
			var customOptions = EasyMwsOptions.Defaults();
			var reportRequestWithRequestRetryPeriodIncomplete = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 2, RequestReportId = null, ReportRequestRetryCount = 1, LastAmazonRequestDate = DateTime.UtcNow.AddHours(-1) };
			customOptions.ReportRequestRetryInitialDelay = TimeSpan.FromHours(2);
			customOptions.ReportRequestRetryInterval = TimeSpan.FromHours(2);
			var reportRequestWithNoRequestRetryCount1 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 3, RequestReportId = null, ReportRequestRetryCount = 0, LastAmazonRequestDate = DateTime.MinValue };
			var reportRequestWithNoRequestRetryCount2 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 4, RequestReportId = null, ReportRequestRetryCount = 0, LastAmazonRequestDate = DateTime.MinValue };

			_reportRequestEntries.Add(reportRequestWithRequestRetryPeriodIncomplete);
			_reportRequestEntries.Add(reportRequestWithNoRequestRetryCount1);
			_reportRequestEntries.Add(reportRequestWithNoRequestRetryCount2);

			var reportRequestCallback =
				_reportRequestEntryService.GetNextFromQueueOfReportsToRequest(customOptions, _merchantId, _amazonRegion);

			Assert.AreEqual(reportRequestWithNoRequestRetryCount1.Id, reportRequestCallback.Id);
		}

		[Test]
		public void GetNextFromQueueOfReportsToRequest_ReturnsFirstReportRequestFromQueueWithCompleteRetryPeriod_AndSkipsReportRequestsWithRequestRetryPeriodIncomplete()
		{
			var customOptions = EasyMwsOptions.Defaults();
			var reportRequestWithRequestRetryPeriodIncomplete = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 2, RequestReportId = null, ReportRequestRetryCount = 1, LastAmazonRequestDate = DateTime.UtcNow.AddMinutes(-30) };
			customOptions.ReportRequestRetryInitialDelay = TimeSpan.FromHours(1);
			customOptions.ReportRequestRetryInterval = TimeSpan.FromHours(1);
			var reportRequestWithNoRetryPeriodComplete1 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 3, RequestReportId = null, ReportRequestRetryCount = 0, LastAmazonRequestDate = DateTime.UtcNow.AddMinutes(-61) };
			var reportRequestWithNoRetryPeriodComplete2 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 4, RequestReportId = null, ReportRequestRetryCount = 0, LastAmazonRequestDate = DateTime.UtcNow.AddMinutes(-61) };

			_reportRequestEntries.Add(reportRequestWithRequestRetryPeriodIncomplete);
			_reportRequestEntries.Add(reportRequestWithNoRetryPeriodComplete1);
			_reportRequestEntries.Add(reportRequestWithNoRetryPeriodComplete2);

			var reportRequestCallback =
				_reportRequestEntryService.GetNextFromQueueOfReportsToRequest(customOptions, _merchantId, _amazonRegion);

			Assert.AreEqual(reportRequestWithNoRetryPeriodComplete1.Id, reportRequestCallback.Id);
		}

		[Test]
		public void GetNextFromQueueOfReportsToRequest_WithConfiguredTimeToWaitBeforeFirstRetry_AndInitialRetryCount_ReturnsReportRequestWithTheExpectedCompleteRetryPeriod()
		{
			var customOptions = EasyMwsOptions.Defaults();
			var reportRequestWithRequestRetryPeriodIncomplete = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 2, RequestReportId = null, ReportRequestRetryCount = 1, LastAmazonRequestDate = DateTime.UtcNow.AddMinutes(-59) };
			customOptions.ReportRequestRetryInitialDelay = TimeSpan.FromMinutes(60);
			customOptions.ReportRequestRetryInterval = TimeSpan.FromMinutes(1);
			var reportRequestWithNoRetryPeriodComplete1 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 3, RequestReportId = null, ReportRequestRetryCount = 1, LastAmazonRequestDate = DateTime.UtcNow.AddMinutes(-61) };
			var reportRequestWithNoRetryPeriodComplete2 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 4, RequestReportId = null, ReportRequestRetryCount = 1, LastAmazonRequestDate = DateTime.UtcNow.AddMinutes(-61) };

			_reportRequestEntries.Add(reportRequestWithRequestRetryPeriodIncomplete);
			_reportRequestEntries.Add(reportRequestWithNoRetryPeriodComplete1);
			_reportRequestEntries.Add(reportRequestWithNoRetryPeriodComplete2);

			var reportRequestCallback = _reportRequestEntryService.GetNextFromQueueOfReportsToRequest(customOptions, _merchantId, _amazonRegion);

			Assert.AreEqual(reportRequestWithNoRetryPeriodComplete1.Id, reportRequestCallback.Id);
		}

		[Test]
		public void GetNextFromQueueOfReportsToRequest_WithRetryPeriodTypeConfiguredAsArithmeticProgression_AndNonInitialRetryCount_ReturnsReportRequestWithTheExpectedCompleteRetryPeriod()
		{
			var customOptions = EasyMwsOptions.Defaults();
			var reportRequestWithRequestRetryPeriodIncomplete = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 2, RequestReportId = null, ReportRequestRetryCount = 5, LastAmazonRequestDate = DateTime.UtcNow.AddMinutes(-59) };
			customOptions.ReportRequestRetryInitialDelay = TimeSpan.FromMinutes(1);
			customOptions.ReportRequestRetryInterval = TimeSpan.FromMinutes(60);
			customOptions.ReportRequestRetryType = RetryPeriodType.ArithmeticProgression;
			var reportRequestWithNoRetryPeriodComplete1 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 3, RequestReportId = null, ReportRequestRetryCount = 5, LastAmazonRequestDate = DateTime.UtcNow.AddMinutes(-61) };
			var reportRequestWithNoRetryPeriodComplete2 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 4, RequestReportId = null, ReportRequestRetryCount = 5, LastAmazonRequestDate = DateTime.UtcNow.AddMinutes(-61) };

			_reportRequestEntries.Add(reportRequestWithRequestRetryPeriodIncomplete);
			_reportRequestEntries.Add(reportRequestWithNoRetryPeriodComplete1);
			_reportRequestEntries.Add(reportRequestWithNoRetryPeriodComplete2);

			var reportRequestCallback = _reportRequestEntryService.GetNextFromQueueOfReportsToRequest(customOptions, _merchantId, _amazonRegion);

			Assert.AreEqual(reportRequestWithNoRetryPeriodComplete1.Id, reportRequestCallback.Id);
		}

		[Test]
		public void GetNextFromQueueOfReportsToRequest_WithRetryPeriodTypeConfiguredAsGeometricProgression_AndNonInitialRetryCount_ReturnsReportRequestWithTheExpectedCompleteRetryPeriod()
		{
			var customOptions = EasyMwsOptions.Defaults();
			var testRequestRetryCount = 5;
			var minutesBetweenRetries = 60;
			var reportRequestWithRequestRetryPeriodIncomplete = new ReportRequestEntry
			{
				AmazonRegion = AmazonRegion.Europe,
				MerchantId = _merchantId,
				Id = 2,
				RequestReportId = null,
				ReportRequestRetryCount = testRequestRetryCount,
				LastAmazonRequestDate = DateTime.UtcNow.AddMinutes(-61)
			};
			customOptions.ReportRequestRetryInitialDelay = TimeSpan.FromMinutes(1);
			customOptions.ReportRequestRetryInterval = TimeSpan.FromMinutes(minutesBetweenRetries);
			customOptions.ReportRequestRetryType = RetryPeriodType.GeometricProgression;
			var reportRequestWithNoRetryPeriodComplete1 = new ReportRequestEntry
			{
				AmazonRegion = AmazonRegion.Europe,
				MerchantId = _merchantId,
				Id = 3,
				RequestReportId = null,
				ReportRequestRetryCount = testRequestRetryCount,
				LastAmazonRequestDate = DateTime.UtcNow.AddMinutes(-59)
			};
			var reportRequestWithNoRetryPeriodComplete2 = new ReportRequestEntry
			{
				AmazonRegion = AmazonRegion.Europe,
				MerchantId = _merchantId,
				Id = 4,
				RequestReportId = null,
				ReportRequestRetryCount = testRequestRetryCount,
				LastAmazonRequestDate = DateTime.UtcNow.AddMinutes(-(testRequestRetryCount * minutesBetweenRetries - 1))
			};
			var reportRequestWithNoRetryPeriodComplete3 = new ReportRequestEntry
			{
				AmazonRegion = AmazonRegion.Europe,
				MerchantId = _merchantId,
				Id = 5,
				RequestReportId = null,
				ReportRequestRetryCount = testRequestRetryCount,
				LastAmazonRequestDate = DateTime.UtcNow.AddMinutes(-(testRequestRetryCount * minutesBetweenRetries - 1))
			};

			_reportRequestEntries.Add(reportRequestWithRequestRetryPeriodIncomplete);
			_reportRequestEntries.Add(reportRequestWithNoRetryPeriodComplete1);
			_reportRequestEntries.Add(reportRequestWithNoRetryPeriodComplete2);
			_reportRequestEntries.Add(reportRequestWithNoRetryPeriodComplete3);

			var reportRequestCallback = _reportRequestEntryService.GetNextFromQueueOfReportsToRequest(customOptions, _merchantId, _amazonRegion);

			Assert.AreEqual(reportRequestWithNoRetryPeriodComplete2.Id, reportRequestCallback.Id);
		}

	    [Test]
	    public void GetNextFromQueueOfReportsToRequest_CalledWithNullMerchantId_ReturnsNull()
	    {
		    var testMerchantId2 = "testMerchantId2";
		    var reportRequestWithDifferentMerchant = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = testMerchantId2, Id = 2, RequestReportId = null, ReportRequestRetryCount = 0 };
		    var reportRequestWithCorrectRegion1 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 3, RequestReportId = null, ReportRequestRetryCount = 0 };
		    var reportRequestWithCorrectRegion2 = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId, Id = 4, RequestReportId = null, ReportRequestRetryCount = 0 };
		    var reportRequestWithNullMerchant = new ReportRequestEntry { AmazonRegion = AmazonRegion.Europe, MerchantId = null, Id = 5, RequestReportId = null, ReportRequestRetryCount = 0 };


		    _reportRequestEntries.Add(reportRequestWithDifferentMerchant);
		    _reportRequestEntries.Add(reportRequestWithCorrectRegion1);
		    _reportRequestEntries.Add(reportRequestWithCorrectRegion2);
		    _reportRequestEntries.Add(reportRequestWithNullMerchant);

		    var reportRequestCallback =
			    _reportRequestEntryService.GetNextFromQueueOfReportsToRequest(_options, null, _amazonRegion);

		    Assert.IsNull(reportRequestCallback);
	    }

	    [Test]
	    public void GetAllPendingReportFromQueue_ForGivenMerchant_ReturnListReportRequestId()
	    {
		    // Arrange
		    var testMerchantId2 = "test merchant id 2";
		    var data = new List<ReportRequestEntry>
		    {
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = testMerchantId2,
				    Id = 2,
				    RequestReportId = "Report2",
				    GeneratedReportId = null
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = testMerchantId2,
				    Id = 3,
				    RequestReportId = "Report3",
				    GeneratedReportId = null
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId,
				    Id = 4,
				    RequestReportId = "Report4",
				    GeneratedReportId = null
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId,
				    Id = 5,
				    RequestReportId = "Report5",
				    GeneratedReportId = null
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId,
				    Id = 6,
				    RequestReportId = "Report6",
				    GeneratedReportId = "GeneratedIdTest1"
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.NorthAmerica, MerchantId = _merchantId,
				    Id = 7,
				    RequestReportId = "Report7",
				    GeneratedReportId = null
			    }
		    };

		    _reportRequestEntries.AddRange(data);

		    // Act
		    var listPendingReports = _reportRequestEntryService.GetAllPendingReportFromQueue(testMerchantId2, _amazonRegion).ToList();

		    // Assert
		    Assert.AreEqual(2, listPendingReports.Count());
		    Assert.IsTrue(listPendingReports.Count(sf => sf == "Report2" || sf == "Report3") == 2);
	    }

	    [Test]
	    public void GetAllPendingReportFromQueue_CalledWithNullMerchantId_ReturnsNull()
	    {
		    var data = new List<ReportRequestEntry>
		    {
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId,
				    Id = 2,
				    RequestReportId = "Report1",
				    GeneratedReportId = null
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = null,
				    Id = 3,
				    RequestReportId = "Report2",
				    GeneratedReportId = null
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = null,
				    Id = 2,
				    RequestReportId = "Report1",
				    GeneratedReportId = null
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = null,
				    Id = 3,
				    RequestReportId = "Report2",
				    GeneratedReportId = null
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = null,
				    Id = 4,
				    RequestReportId = "Report3",
				    GeneratedReportId = "GeneratedIdTest1"
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.NorthAmerica, MerchantId = null,
				    Id = 5,
				    RequestReportId = "Report4",
				    GeneratedReportId = null
			    }
		    };

		    _reportRequestEntries.AddRange(data);

		    // Act
		    var listPendingReports = _reportRequestEntryService.GetAllPendingReportFromQueue(null, _amazonRegion);

		    // Assert
		    Assert.IsEmpty(listPendingReports);

	    }

	    [Test]
	    public void GetNextFromQueueOfReportsToDownload_CalledWithNullMerchantId_ReturnsNull()
	    {
		    // Arrange
		    var data = new List<ReportRequestEntry>
		    {
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = _merchantId,
				    Id = 2,
				    RequestReportId = "Report1",
				    GeneratedReportId = null
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = null,
				    Id = 3,
				    RequestReportId = "Report2",
				    GeneratedReportId = "GeneratedIdTest2"
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = null,
				    Id = 4,
				    RequestReportId = "Report2",
				    GeneratedReportId = "GeneratedIdTest2"
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.Europe, MerchantId = null,
				    Id = 5,
				    RequestReportId = "Report3",
				    GeneratedReportId = "GeneratedIdTest3"
			    },
			    new ReportRequestEntry
			    {
				    AmazonRegion = AmazonRegion.NorthAmerica, MerchantId = null,
				    Id = 6,
				    RequestReportId = "Report4",
				    GeneratedReportId = null
			    }
		    };
		    _reportRequestEntries.AddRange(data);

		    var result = _reportRequestEntryService.GetNextFromQueueOfReportsToDownload(_options, null, _amazonRegion);

		    Assert.IsNull(result);
	    }
	}
}
