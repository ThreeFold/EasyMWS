﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MountainWarehouse.EasyMWS.CallbackLogic;
using MountainWarehouse.EasyMWS.Enums;
using MountainWarehouse.EasyMWS.Model;
using Newtonsoft.Json;

namespace MountainWarehouse.EasyMWS.Data
{
    public class FeedSubmissionEntry
    {
	    private string _regionAndType;

	    [NotMapped]
	    public string RegionAndTypeComputed
	    {
		    // this field is populated based on ReportRequestData which, once set in the ctor, should never change again for the same entity.
			get { return _regionAndType = _regionAndType ?? $"(FeedType:{FeedType},Region:{AmazonRegion.ToString()})"; }
	    }

	    [Key]
	    public int Id { get; set; }
		public bool IsLocked { get; set; }
	    public int FeedSubmissionRetryCount { get; set; }
	    public int FeedProcessingRetryCount { get; set; }
	    public int ReportDownloadRetryCount { get; set; }
	    public int InvokeCallbackRetryCount { get; set; }
		public DateTime LastSubmitted { get; set; }
        public string LastAmazonFeedProcessingStatus { get; set; }
        public DateTime DateCreated { get; set; }

		#region Serialized callback data necessary to invoke a method with it's argument values.
		public string TypeName { get; set; }
	    public string MethodName { get; set; }
	    public string Data { get; set; }
	    public string DataTypeName { get; set; }
		#endregion

		#region Data necessary to request a report from amazon.
	    public AmazonRegion AmazonRegion { get; set; }
	    public string FeedType { get; set; }
	    public string MerchantId { get; set; }
		public string FeedSubmissionData { get; set; }
		#endregion

		#region Additional data generated by amazon in the process of fetching reports

	    public string FeedSubmissionId { get; set; }
	    public bool IsProcessingComplete { get; set; }
	    public bool HasErrors { get; set; }
	    public string SubmissionErrorData { get; set; }


	    public virtual FeedSubmissionDetails Details { get; set; }

	    #endregion

		[Obsolete("This constructor should never be used directly. But it has to exist as required by EF. Use other overloads instead!")]
		public FeedSubmissionEntry()
		{
		}

	    public FeedSubmissionEntry(string feedSubmissionData, Callback callback = null)
	    {
			if( string.IsNullOrEmpty(feedSubmissionData))
				throw new ArgumentException("Callback data or FeedSubmissionData not provided, but are required");

		    TypeName = callback?.TypeName;
		    MethodName = callback?.MethodName;
		    Data = callback?.Data;
		    DataTypeName = callback?.DataTypeName;
		    FeedSubmissionData = feedSubmissionData;
            LastAmazonFeedProcessingStatus = null;
        }
	}

	internal static class FeedSubmissionCallbackExtensions
	{
		internal static FeedSubmissionPropertiesContainer GetPropertiesContainer(this FeedSubmissionEntry source)
		{
			return JsonConvert.DeserializeObject<FeedSubmissionPropertiesContainer>(source.FeedSubmissionData);
		}
	}
}
