﻿using System.Collections.Generic;
using MountainWarehouse.EasyMWS.Data;

namespace MountainWarehouse.EasyMWS.ReportProcessors
{
    internal interface IFeedSubmissionProcessor
    {
	    FeedSubmissionCallback GetNextFeedToSubmitFromQueue(AmazonRegion region, string merchantId);
	    string SubmitSingleQueuedFeedToAmazon(FeedSubmissionCallback feedSubmission, string merchantId);
	    void AllocateFeedSubmissionForRetry(FeedSubmissionCallback feedSubmission);
	    void MoveToQueueOfSubmittedFeeds(FeedSubmissionCallback feedSubmission, string feedSubmissionId);
		IEnumerable<FeedSubmissionCallback> GetAllSubmittedFeeds(AmazonRegion region, string merchantId);

	    List<(string FeedSubmissionId, string FeedProcessingStatus)> GetFeedSubmissionResults(
		    IEnumerable<string> feedSubmissionIdList, string merchant);

	    void MoveFeedsToProcessedQueue(List<(string FeedSubmissionId, string FeedProcessingStatus)> feedProcessingStatuses);
	    void ReturnFeedsToProcessingRetryQueue(List<(string FeedSubmissionId, string FeedProcessingStatus)> feedProcessingStatuses);
	}
}
