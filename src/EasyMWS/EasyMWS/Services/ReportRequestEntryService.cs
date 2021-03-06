﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MountainWarehouse.EasyMWS.Data;
using MountainWarehouse.EasyMWS.Enums;
using MountainWarehouse.EasyMWS.Helpers;
using MountainWarehouse.EasyMWS.Logging;
using MountainWarehouse.EasyMWS.Model;
using MountainWarehouse.EasyMWS.Repositories;

namespace MountainWarehouse.EasyMWS.Services
{
	internal class ReportRequestEntryService : IReportRequestEntryService, IDisposable
	{
		private readonly IReportRequestEntryRepository _reportRequestEntryRepository;
		private readonly IEasyMwsLogger _logger;
		private readonly EasyMwsOptions _options;

		internal ReportRequestEntryService(IReportRequestEntryRepository reportRequestEntryRepository, EasyMwsOptions options = null, IEasyMwsLogger logger = null) : this(options, logger)
			=> (_reportRequestEntryRepository) = (reportRequestEntryRepository);

		internal ReportRequestEntryService(EasyMwsOptions options = null, IEasyMwsLogger logger = null) => (_reportRequestEntryRepository, _logger, _options) =
			(_reportRequestEntryRepository ?? new ReportRequestEntryRepository(options?.LocalDbConnectionStringOverride), logger, options);

		public void Create(ReportRequestEntry entry) => _reportRequestEntryRepository.Create(entry);
		public async Task CreateAsync(ReportRequestEntry entry) => await _reportRequestEntryRepository.CreateAsync(entry);
		public void Update(ReportRequestEntry entry) => _reportRequestEntryRepository.Update(entry);
		public void Delete(ReportRequestEntry entry)
		{
			try
			{
				_reportRequestEntryRepository.Delete(entry);
			}
			catch (Exception e)
			{
				_logger.Error(!_reportRequestEntryRepository.GetAll().Where(rr => rr.Id == entry.Id).Select(r => r.Id).Any()
						? $"Delete ReportRequestCallback entity with ID: {entry.Id} failed. It is likely the entity has already been deleted"
						: $"Delete ReportRequestCallback entity with ID: {entry.Id} failed. See exception info for more details", e);
			}

		}

		public void DeleteRange(IEnumerable<ReportRequestEntry> entries)
		{
			_reportRequestEntryRepository.DeleteRange(entries);
		}

		public void SaveChanges() => _reportRequestEntryRepository.SaveChanges();
		public async Task SaveChangesAsync() => await _reportRequestEntryRepository.SaveChangesAsync();
		public IEnumerable<ReportRequestEntry> GetAll() => _reportRequestEntryRepository.GetAll().OrderBy(x => x.Id);

		public IEnumerable<ReportRequestEntry> Where(Func<ReportRequestEntry, bool> predicate) => _reportRequestEntryRepository.GetAll().OrderBy(x => x.Id).Where(predicate);

		public ReportRequestEntry First() => _reportRequestEntryRepository.GetAll().OrderBy(x => x.Id).First();
		public ReportRequestEntry FirstOrDefault() => _reportRequestEntryRepository.GetAll().OrderBy(x => x.Id).FirstOrDefault();
		public ReportRequestEntry FirstOrDefault(Func<ReportRequestEntry, bool> predicate) => _reportRequestEntryRepository.GetAll().OrderBy(x => x.Id).FirstOrDefault(predicate);

		public ReportRequestEntry GetNextFromQueueOfReportsToRequest(string merchantId, AmazonRegion region, bool markEntryAsLocked = true)
		{
			var entry = FirstOrDefault(rre => rre.AmazonRegion == region && rre.MerchantId == merchantId
											 && rre.RequestReportId == null
											 && RetryIntervalHelper.IsRetryPeriodAwaited(rre.LastAmazonRequestDate, rre.ReportRequestRetryCount,
									   _options.ReportRequestRetryInitialDelay, _options.ReportRequestRetryInterval,
									   _options.ReportRequestRetryType)
									   && rre.IsLocked == false);

			if (entry != null && markEntryAsLocked)
			{
				entry.IsLocked = true;
				Update(entry);
				SaveChanges();
			}

			return entry;
		}


		public ReportRequestEntry GetNextFromQueueOfReportsToDownload(string merchantId, AmazonRegion region, bool markEntryAsLocked = true)
		{
			var entry = FirstOrDefault(rre => rre.AmazonRegion == region && rre.MerchantId == merchantId
							 && rre.RequestReportId != null && rre.GeneratedReportId != null && rre.Details == null && rre.LastAmazonReportProcessingStatus != AmazonReportProcessingStatus.DoneNoData
                             && RetryIntervalHelper.IsRetryPeriodAwaited(rre.LastAmazonRequestDate, rre.ReportDownloadRetryCount,
									   _options.ReportDownloadRetryInitialDelay, _options.ReportDownloadRetryInterval,
									   _options.ReportDownloadRetryType)
									   && rre.IsLocked == false);

			if (entry != null && markEntryAsLocked)
			{
				entry.IsLocked = true;
				Update(entry);
				SaveChanges();
			}

			return entry;
		}

		public IEnumerable<string> GetAllPendingReportFromQueue(string merchantId, AmazonRegion region, bool markEntriesAsLocked = true)
		{
			var entries = Where(rre => rre.AmazonRegion == region && rre.MerchantId == merchantId
									 && rre.RequestReportId != null && rre.GeneratedReportId == null && rre.IsLocked == false);
			var entriesIds = entries.Select(r => r.RequestReportId).ToList();

			if (entries.Any() && markEntriesAsLocked)
			{
				foreach (var entry in entries)
				{
					entry.IsLocked = true;
					Update(entry);
				}
				SaveChanges();
			}

			return entriesIds;
		}

		public IEnumerable<ReportRequestEntry> GetAllFromQueueOfReportsReadyForCallback(string merchantId, AmazonRegion region, bool markEntriesAsLocked = true)
		{
			var entries = Where(rre => rre.AmazonRegion == region && rre.MerchantId == merchantId
							  && (rre.Details != null || rre.LastAmazonReportProcessingStatus == AmazonReportProcessingStatus.DoneNoData)
							  && RetryIntervalHelper.IsRetryPeriodAwaited(rre.LastAmazonRequestDate, rre.InvokeCallbackRetryCount,
							   _options.InvokeCallbackRetryInterval, _options.InvokeCallbackRetryInterval,
							   _options.InvokeCallbackRetryPeriodType) && rre.IsLocked == false).ToList();

			if (entries.Any() && markEntriesAsLocked)
			{
				foreach (var entry in entries)
				{
					entry.IsLocked = true;
					Update(entry);
				}
				SaveChanges();
			}

			return entries;
		}

		public void Dispose()
		{
			_reportRequestEntryRepository.Dispose();
		}
	}
}
