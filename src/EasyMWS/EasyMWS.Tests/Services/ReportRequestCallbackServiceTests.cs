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
using MountainWarehouse.EasyMWS.Repositories;
using MountainWarehouse.EasyMWS.Services;
using Newtonsoft.Json;
using NUnit.Framework;

namespace EasyMWS.Tests.Services
{
	[TestFixture]
    public class ReportRequestCallbackServiceTests
    {
	    private Mock<IReportRequestCallbackRepo> _reportRequestCallbackReportMock;
	    private ReportRequestCallbackService _reportRequestCallbackService;

	    [SetUp]
	    public void SetUp()
	    {
		    var reportRequestPropertiesContainer = new ReportRequestPropertiesContainer("_Report_Type_", ContentUpdateFrequency.NearRealTime, new List<string>(MwsMarketplaceGroup.AmazonEurope()));

		    var reportRequestCallback = new List<ReportRequestCallback>
		    {
			    new ReportRequestCallback
				{
					AmazonRegion = AmazonRegion.Europe,
					Data = "testData",
					ReportRequestData = JsonConvert.SerializeObject(reportRequestPropertiesContainer),
					MethodName = "testMethodName",
					TypeName = "testTypeName",
					LastRequested = DateTime.MinValue,
					DataTypeName = "testDataTypeName",
					ContentUpdateFrequency = 0,
					Id = 1
				},
				new ReportRequestCallback{Id = 2}
		    };

			_reportRequestCallbackReportMock = new Mock<IReportRequestCallbackRepo>();
		    _reportRequestCallbackReportMock.Setup(x => x.GetAll()).Returns(reportRequestCallback.AsQueryable());
			_reportRequestCallbackService = new ReportRequestCallbackService(_reportRequestCallbackReportMock.Object);
	    }


		[Test]
		public void FirstOrDefault_TwoInQueue_ReturnsFirstObjectContainingCorrectData()
		{
			var reportRequestCallback = _reportRequestCallbackService.FirstOrDefault();
			var reportRequestData = JsonConvert.DeserializeObject<ReportRequestPropertiesContainer>(reportRequestCallback.ReportRequestData);

			Assert.AreEqual(AmazonRegion.Europe, reportRequestCallback.AmazonRegion);
			Assert.AreEqual("testData", reportRequestCallback.Data);
			Assert.AreEqual("testMethodName", reportRequestCallback.MethodName);
			Assert.AreEqual("testTypeName", reportRequestCallback.TypeName);
			Assert.AreEqual("testDataTypeName", reportRequestCallback.DataTypeName);
			Assert.AreEqual("_Report_Type_", reportRequestData.ReportType);
			Assert.AreEqual(ContentUpdateFrequency.NearRealTime, reportRequestData.UpdateFrequency);
			CollectionAssert.AreEquivalent(new List<string>(MwsMarketplaceGroup.AmazonEurope()), reportRequestData.MarketplaceIdList);
		}
	}
}
