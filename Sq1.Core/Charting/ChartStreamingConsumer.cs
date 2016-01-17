﻿using System;

using Sq1.Core.StrategyBase;
using Sq1.Core.DataTypes;
using Sq1.Core.DataFeed;
using Sq1.Core.Streaming;
using Sq1.Core.Execution;

namespace Sq1.Core.Charting {
	public class ChartStreamingConsumer : IStreamingConsumer {
		string	msigForNpExceptions = "Failed to StreamingSubscribe(): ";
				ChartShadow chartShadow;

		#region CASCADED_INITIALIZATION_ALL_CHECKING_CONSISTENCY_FROM_ONE_METHOD begin
		ScriptExecutor Executor_nullReported { get {
				var ret = this.chartShadow.Executor;
				this.actionForNullPointer(ret, "this.chartShadow.Executor=null");
				return ret;
			} }
		Strategy Strategy_nullReported { get {
				var ret = this.Executor_nullReported.Strategy;
				this.actionForNullPointer(ret, "this.chartShadow.Executor.Strategy=null");
				return ret;
			} }
		ContextChart ContextCurrentChartOrStrategy_nullReported { get {
				var ret = this.Strategy_nullReported.ScriptContextCurrent;
				this.actionForNullPointer(ret, "this.chartShadow.Executor.Strategy.ScriptContextCurrent=null");
				return ret;
			} }
		string Symbol_nullReported { get {
				string symbol = (this.Executor_nullReported.Strategy == null) ? this.Executor_nullReported.Bars.Symbol : this.ContextCurrentChartOrStrategy_nullReported.Symbol;
				if (String.IsNullOrEmpty(symbol)) {
					this.action("this.chartShadow.Executor.Strategy.ScriptContextCurrent.Symbol IsNullOrEmpty");
				}
				return symbol;
			} }
		BarScaleInterval ScaleInterval_nullReported { get {
				var ret = (this.Executor_nullReported.Strategy == null) ? this.Executor_nullReported.Bars.ScaleInterval : this.ContextCurrentChartOrStrategy_nullReported.ScaleInterval;
				this.actionForNullPointer(ret, "this.chartShadow.Executor.Strategy.ScriptContextCurrent.ScaleInterval=null");
				return ret;
			} }
		BarScale Scale_nullReported { get {
				var ret = this.ScaleInterval_nullReported.Scale;
				this.actionForNullPointer(ret, "this.chartShadow.Executor.Strategy.ScriptContextCurrent.ScaleInterval.Scale=null");
				if (ret == BarScale.Unknown) {
					this.action("this.chartShadow.Executor.Strategy.ScriptContextCurrent.ScaleInterval.Scale=Unknown");
				}
				return ret;
			} }
		//		BarDataRange DataRange { get {
		//				var ret = this.ScriptContextCurrent.DataRange;
		//				this.actionForNullPointer(ret, "this.chartShadow.Executor.Strategy.ScriptContextCurrent.DataRange=null");
		//				return ret;
		//			} }
		DataSource DataSource_nullReported { get {
				var ret = this.Executor_nullReported.DataSource;
				this.actionForNullPointer(ret, "this.chartShadow.Executor.DataSource=null");
				return ret;
			} }
		StreamingAdapter StreamingAdapter_nullReported { get {
				StreamingAdapter ret = this.DataSource_nullReported.StreamingAdapter;
				this.actionForNullPointer(ret, "this.chartShadow.Executor.DataSource[" + this.DataSource_nullReported + "].StreamingAdapter=null STREAMING_ADAPDER_NOT_ASSIGNED_IN_DATASOURCE");
				return ret;
			} }
		StreamingSolidifier StreamingSolidifierDeep { get {
				var ret = this.StreamingAdapter_nullReported.StreamingSolidifier;
				this.actionForNullPointer(ret, "this.chartShadow.Executor.DataSource[" + this.DataSource_nullReported + "].StreamingAdapter.StreamingSolidifier=null");
				return ret;
			} }

		ChartShadow ChartShadow_nullReported { get {
				var ret = this.chartShadow;
				this.actionForNullPointer(ret, "this.chartShadow=null");
				return ret;
			} }
		Bars Bars_nullReported { get {
				var ret = this.Executor_nullReported.Bars;
				this.actionForNullPointer(ret, "this.chartShadow.Executor.Bars=null");
				return ret;
			} }
		Bar StreamingBarSafeClone_nullReported { get {
				var ret = this.Bars_nullReported.BarStreamingNullUnsafeCloneReadonly;
				//this.actionForNullPointer(ret, "this.chartShadow.Executor.Bars.StreamingBarSafeClone=null");
				if (ret == null) ret = new Bar();
				return ret;
			} }
		Bar LastStaticBar_nullReported { get {
				var ret = this.Bars_nullReported.BarStaticLastNullUnsafe;
				this.actionForNullPointer(ret, "this.chartShadow.Executor.Bars.LastStaticBar=null");
				return ret;
			} }
		void actionForNullPointer(object mustBeInstance, string msgIfNull) {
			if (mustBeInstance != null) return;
			this.action(msgIfNull);
		}
		void action(string msgIfNull) {
			string msg = msigForNpExceptions + msgIfNull;
			Assembler.PopupException(msg, null, false);
			//throw new Exception(msg);
		}
		bool canSubscribeToStreamingAdapter() {
			try {
				var symbolSafe		= this.Symbol_nullReported;
				var scaleSafe		= this.Scale_nullReported;
				var streamingSafe	= this.StreamingAdapter_nullReported;
				var staticDeepSafe	= this.StreamingSolidifierDeep;
			} catch (Exception ex) {
				// already reported
				return false;
			}
			return true;
		}
		#endregion

		public ChartStreamingConsumer(ChartShadow chartShadow) {
			this.chartShadow = chartShadow;
		}

		public void StreamingUnsubscribe(string reason = "NO_REASON_FOR_STREAMING_UNSUBSCRIBE") {
			this.msigForNpExceptions = " //ChartStreamingConsumer.StreamingUnsubscribe(" + this.ToString() + ")";

			var executorSafe			= this.Executor_nullReported;
			var symbolSafe				= this.Symbol_nullReported;
			var scaleIntervalSafe		= this.ScaleInterval_nullReported;
			var streaming_nullReported	= this.StreamingAdapter_nullReported;

			bool downstreamSubscribed = this.DownstreamSubscribed;
			if (downstreamSubscribed == false) {
				string msg = "CHART_STREAMING_ALREADY_UNSUBSCRIBED_QUOTES_AND_BARS";
				Assembler.PopupException(msg + this.msigForNpExceptions, null, false);
				// RESET_IsStreaming=subscribed return;
			}

			if (streaming_nullReported != null) {
				string branch = " DATA_SOURCE_HAS_STREAMING_ASSIGNED_1/2";
				streaming_nullReported.UnsubscribeChart(symbolSafe, scaleIntervalSafe, this, branch + this.msigForNpExceptions);

				//re-reading to be 100% sure
				downstreamSubscribed = this.DownstreamSubscribed;
				if (downstreamSubscribed) {
					string msg = "ERROR_CHART_STREAMING_STILL_SUBSCRIBED_QUOTES_OR_BARS";
					Assembler.PopupException(msg + this.msigForNpExceptions);
					return;
				}
			} else {
				string msg = "ChartForm: BARS=>UNSUBSCRIBE_SHOULD_DISABLED_KOZ_NO_STREAMING_IN_DATASOURCE in PopulateBtnStreamingTriggersScript_afterBarsLoaded()";
				Assembler.PopupException(msg);
			}

			this.ContextCurrentChartOrStrategy_nullReported.DownstreamSubscribed = downstreamSubscribed;
			this.Strategy_nullReported.Serialize();

			string msg2 = "CHART_STREAMING_UNSUBSCRIBED[" + downstreamSubscribed + "] due to [" + reason + "]";
			Assembler.PopupException(msg2 + this.msigForNpExceptions, null, false);

			// TUNNELLED_TO_CHART_CONTROL this.ChartShadow_nullReported.ChartControl.ScriptExecutorObjects.QuoteLast = null;
		}
		public void StreamingSubscribe(string reason = "NO_REASON_FOR_STREAMING_SUBSCRIBE") {
			if (this.canSubscribeToStreamingAdapter() == false) return;	// NULL_POINTERS_ARE_ALREADY_REPORTED_TO_EXCEPTIONS_FORM
			this.msigForNpExceptions = " //ChartStreamingConsumer.StreamingSubscribe(" + this.ToString() + ")";

			var executorSafe				= this.Executor_nullReported;
			var symbolSafe					= this.Symbol_nullReported;
			var scaleIntervalSafe			= this.ScaleInterval_nullReported;
			var streaming_nullReported		= this.StreamingAdapter_nullReported;
			var streamingBarSafeCloneSafe	= this.StreamingBarSafeClone_nullReported;

			bool downstreamSubscribed = this.DownstreamSubscribed;
			if (downstreamSubscribed) {
				string msg = "CHART_STREAMING_ALREADY_SUBSCRIBED_OR_FORGOT_TO_DISCONNECT REMOVE_INVOCATION_UPSTACK";
				Assembler.PopupException(msg + this.msigForNpExceptions, null, false);
				// RESET_IsStreaming=subscribed return;
			}

			if (streaming_nullReported != null) {
				string branch = " DATA_SOURCE_HAS_STREAMING_ASSIGNED_1/2";

				// I dont remember what I was testing disabled
				//Assembler.PopupException("Subscribing BarsConsumer [" + this + "] to " + this.ToString() + " (wasn't registered)");
				//if (executorSafe.Bars == null) {
				//    string msg = "in Initialize() this.ChartForm is requesting bars in a separate thread";
				//    Assembler.PopupException(msg);
				//} else {
				//    string msg = "fully initialized, after streaming was stopped for a moment and resumed - append into PartialBar";
				//    Assembler.PopupException(msg);
				//}

				streaming_nullReported.SubscribeChart(symbolSafe, scaleIntervalSafe, this, branch + this.msigForNpExceptions);

				//re-reading to be 100% sure
				downstreamSubscribed = this.DownstreamSubscribed;
				if (downstreamSubscribed == false) {
					string msg = "CHART_STREAMING_FAILED_SUBSCRIBE_BAR_OR_QUOTE_OR_BOTH StreamingAdapter[" + streaming_nullReported.ToString() + "]";
					Assembler.PopupException(msg + this.msigForNpExceptions);
					return;
				}
			} else {
				string msg = "ChartForm: BARS=>SUBSCRIBE_SHOULD_BE_DISABLED_KOZ_NO_STREAMING_IN_DATASOURCE in PopulateBtnStreamingTriggersScript_afterBarsLoaded()";
				Assembler.PopupException(msg);
			}

			this.ContextCurrentChartOrStrategy_nullReported.DownstreamSubscribed = downstreamSubscribed;
			this.Strategy_nullReported.Serialize();

			string msg2 = "CHART_STREAMING_SUBSCRIBED[" + downstreamSubscribed + "] due to [" + reason + "]";
			Assembler.PopupException(msg2 + this.msigForNpExceptions, null, false);
		}
		public bool DownstreamSubscribed { get {
				if (this.canSubscribeToStreamingAdapter() == false) return false;	// NULL_POINTERS_ARE_ALREADY_REPORTED_TO_EXCEPTIONS_FORM

				var streamingSafe		= this.StreamingAdapter_nullReported;
				var symbolSafe			= this.Symbol_nullReported;
				var scaleIntervalSafe	= this.ScaleInterval_nullReported;

				bool quote	= streamingSafe.DataDistributor.ConsumerQuoteIsSubscribed(	symbolSafe, scaleIntervalSafe, this);
				bool bar	= streamingSafe.DataDistributor.ConsumerBarIsSubscribed(	symbolSafe, scaleIntervalSafe, this);
				bool ret = quote & bar;
				return ret;
			}}

		public void StreamingTriggeringScriptStart() {
			this.Executor_nullReported.IsStreamingTriggeringScript = true;
		}
		public void StreamingTriggeringScriptStop() {
			this.Executor_nullReported.IsStreamingTriggeringScript = false;
		}

		#region IStreamingConsumer
		Bars IStreamingConsumer.ConsumerBarsToAppendInto { get { return this.Bars_nullReported; } }
		void IStreamingConsumer.UpstreamSubscribedToSymbolNotification(Quote quoteFirstAfterStart) {
		}
		void IStreamingConsumer.UpstreamUnSubscribedFromSymbolNotification(Quote quoteLastBeforeStop) {
		}
		void IStreamingConsumer.ConsumeBarLastStaticJustFormedWhileStreamingBarWithOneQuoteAlreadyAppended(Bar barLastFormed, Quote quoteForAlertsCreated) {
			if (barLastFormed == null) {
				string msg = "WRONG_SHOW_BRO";
				Assembler.PopupException(msg);
			}
			this.msigForNpExceptions = " //ChartStreamingConsumer.ConsumeBarLastStaticJustFormedWhileStreamingBarWithOneQuoteAlreadyAppended(" + barLastFormed.ToString() + ")";

			#if DEBUG	// TEST_INLINE
			var barsSafe = this.Bars_nullReported;
			if (barsSafe.ScaleInterval != barLastFormed.ScaleInterval) {
				string msg = "SCALEINTERVAL_RECEIVED_DOESNT_MATCH_CHARTS ChartForm[" + this.ChartShadow_nullReported.Text + "]"
					+ " bars[" + barsSafe.ScaleInterval + "] barLastFormed[" + barLastFormed.ScaleInterval + "]";
				Assembler.PopupException(msg + this.msigForNpExceptions);
				return;
			}
			if (barsSafe.Symbol != barLastFormed.Symbol) {
				string msg = "SYMBOL_RECEIVED_DOESNT_MATCH_CHARTS ChartForm[" + this.ChartShadow_nullReported.Text + "]"
					+ " bars[" + barsSafe.Symbol + "] barLastFormed[" + barLastFormed.Symbol + "]";
				Assembler.PopupException(msg + this.msigForNpExceptions);
				return;
			}
			#endif

			var chartFormSafe		= this.ChartShadow_nullReported;
			var executorSafe		= this.Executor_nullReported;
			var dataSourceSafe		= this.DataSource_nullReported;

			if (barLastFormed == null) {
				string msg = "Streaming starts generating quotes => first StreamingBar is added; for first four Quotes there's no static barsFormed yet!! Isi";
				Assembler.PopupException(msg + this.msigForNpExceptions);
				return;
			}

			if (executorSafe.Strategy != null && executorSafe.IsStreamingTriggeringScript) {
				try {
					bool thereWereNeighbours = dataSourceSafe.PumpPauseNeighborsIfAnyFor(executorSafe);		// NOW_FOR_LIVE_MOCK_BUFFERING
					// TESTED BACKLOG_GREWUP Thread.Sleep(450);	// 10,000msec = 10sec
					ReporterPokeUnit pokeUnitNullUnsafe = executorSafe.ExecuteOnNewBarOrNewQuote(quoteForAlertsCreated, false);	//new Quote());
					//UNFILLED_POSITIONS_ARE_USELESS chartFormManager.ReportersFormsManager.BuildIncrementalAllReports(pokeUnit);
				} finally {
					bool thereWereNeighbours = dataSourceSafe.PumpResumeNeighborsIfAnyFor(executorSafe);	// NOW_FOR_LIVE_MOCK_BUFFERING
				}
			}

			#if DEBUG
			if (this.Executor_nullReported.Backtester.IsBacktestingNoLivesimNow) {
				string msg = "SHOULD_NEVER_HAPPEN IsBacktestingNoLivesimNow[true] //ChartStreamingConsumer.ConsumeBarLastStaticJustFormedWhileStreamingBarWithOneQuoteAlreadyAppended()";
				Assembler.PopupException(msg);
				return;
			}
			#endif

			if (this.ContextCurrentChartOrStrategy_nullReported.DownstreamSubscribed) {
				chartFormSafe.InvalidateAllPanels();
			}
		}
		void IStreamingConsumer.ConsumeQuoteOfStreamingBar(Quote quote) {
			this.msigForNpExceptions = " //ChartStreamingConsumer.ConsumeQuoteOfStreamingBar(" + quote.ToString() + ")";

			#if DEBUG	// TEST_INLINE_BEGIN
			var barsSafe = this.Bars_nullReported;
			if (barsSafe.ScaleInterval != quote.ParentBarStreaming.ScaleInterval) {
				string msg = "SCALEINTERVAL_RECEIVED_DOESNT_MATCH_CHARTS ChartForm[" + this.ChartShadow_nullReported.Text + "]"
					+ " bars[" + barsSafe.ScaleInterval + "] quote.ParentStreamingBar[" + quote.ParentBarStreaming.ScaleInterval + "]";
				Assembler.PopupException(msg + this.msigForNpExceptions);
				return;
			}
			if (barsSafe.Symbol != quote.ParentBarStreaming.Symbol) {
				string msg = "SYMBOL_RECEIVED_DOESNT_MATCH_CHARTS ChartForm[" + this.ChartShadow_nullReported.Text + "]"
					+ " bars[" + barsSafe.Symbol + "] quote.ParentStreamingBar[" + quote.ParentBarStreaming.Symbol + "]";
				Assembler.PopupException(msg + this.msigForNpExceptions);
				return;
			}
			string msg2 = "BARS_IDENTICAL";
			bool sameDOHLCV = barsSafe.BarStreamingNullUnsafe.HasSameDOHLCVas(quote.ParentBarStreaming, "quote.ParentStreamingBar", "barsSafe.BarStreaming", ref msg2);
			if (sameDOHLCV == false) {
				string msg = "FIXME_MUST_BE_THE_SAME EARLY_BINDER_DIDNT_DO_ITS_JOB#3 [" + msg2 + "] this.Executor.Bars.BarStreaming[" + barsSafe.BarStreamingNullUnsafe
					+ "].HasSameDOHLCVas(quote.ParentStreamingBar[" + quote.ParentBarStreaming + "])=false";
				Assembler.PopupException(msg + this.msigForNpExceptions);
				return;
			}
			if (barsSafe.BarStreamingNullUnsafe != quote.ParentBarStreaming) {
				string msg = "SHOULD_THEY_BE_CLONES_OR_SAME? EARLY_BINDER_DIDNT_DO_ITS_JOB#3 bars[" + barsSafe
					+ "] quote.ParentStreamingBar[" + quote.ParentBarStreaming + "]";
				Assembler.PopupException(msg + this.msigForNpExceptions);
				return;
			}
			#endif	// TEST_INLINE_END

			var streamingSafe	= this.StreamingAdapter_nullReported;
			var chartFormSafe	= this.ChartShadow_nullReported;
			var executorSafe	= this.Executor_nullReported;
			var dataSourceSafe	= this.DataSource_nullReported;

			// STREAMING_BAR_IS_ALREADY_MERGED_IN_EARLY_BINDER_WITH_QUOTE_RECIPROCALLY
			//try {
			//	streamingSafe.InitializeStreamingOHLCVfromStreamingAdapter(this.chartFormManager.Executor.Bars);
			//} catch (Exception e) {
			//	Assembler.PopupException("didn't merge with Partial, continuing", e, false);
			//}

			if (quote.ParentBarStreaming.ParentBarsIndex > quote.ParentBarStreaming.ParentBars.Count) {
				string msg = "should I add a bar into Chart.Bars?... NO !!! already added";
			}

			// #1/4 launch update in GUI thread
			//MOVED_TO_chartControl_BarAddedUpdated_ShouldTriggerRepaint chartFormSafe.ChartControl.ScriptExecutorObjects.QuoteLast = quote.Clone();
			// TUNNELLED_TO_CHART_FORMS_MANAGER chartFormSafe.PrintQuoteTimestampOnStrategyTriggeringButton_beforeExecution_switchToGuiThread(quote);
			executorSafe.EventGenerator.RaiseOnStrategyPreExecuteOneQuote(quote);

			// #2/4 execute strategy in the thread of a StreamingAdapter (DDE server for MockQuickProvider)
			if (executorSafe.Strategy != null) {
				if (executorSafe.IsStreamingTriggeringScript) {
					try {
						bool thereWereNeighbours = dataSourceSafe.PumpPauseNeighborsIfAnyFor(executorSafe);		// NOW_FOR_LIVE_MOCK_BUFFERING
						// TESTED BACKLOG_GREWUP Thread.Sleep(450);	// 10,000msec = 10sec
						ReporterPokeUnit pokeUnitNullUnsafe1 = executorSafe.ExecuteOnNewBarOrNewQuote(quote, true);
						//UNFILLED_POSITIONS_ARE_USELESS chartFormManager.ReportersFormsManager.BuildIncrementalAllReports(pokeUnit);
					} finally {
						bool thereWereNeighbours = dataSourceSafe.PumpResumeNeighborsIfAnyFor(executorSafe);	// NOW_FOR_LIVE_MOCK_BUFFERING
					}
				} else {
					// UPDATE_REPORTS_OPEN_POSITIONS_WITH_EACH_QUOTE_DESPITE_STRATEGY_IS_NOT_TRIGGERED
					// copypaste from Executor.ExecuteOnNewBarOrNewQuote()
					ReporterPokeUnit pokeUnit = new ReporterPokeUnit(quote,
						executorSafe.ExecutionDataSnapshot.AlertsNewAfterExec		.Clone(this, "ConsumeQuoteOfStreamingBar(WAIT)"),
						executorSafe.ExecutionDataSnapshot.PositionsOpenedAfterExec	.Clone(this, "ConsumeQuoteOfStreamingBar(WAIT)"),
						executorSafe.ExecutionDataSnapshot.PositionsClosedAfterExec	.Clone(this, "ConsumeQuoteOfStreamingBar(WAIT)"),
						executorSafe.ExecutionDataSnapshot.PositionsOpenNow			.Clone(this, "ConsumeQuoteOfStreamingBar(WAIT)")
					);

					// FROM_ChartStreamingConsumer.ConsumeQuoteOfStreamingBar() #4/4 notify Positions that it should update open positions, I wanna see current profit/loss and relevant red/green background
					if (pokeUnit.PositionsOpenNow.Count > 0) {
						executorSafe.PerformanceAfterBacktest.BuildIncrementalOpenPositionsUpdatedDueToStreamingNewQuote_step2of3(executorSafe.ExecutionDataSnapshot.PositionsOpenNow);
						executorSafe.EventGenerator.RaiseOpenPositionsUpdatedDueToStreamingNewQuote_step2of3(pokeUnit);
					}
				}
			}

			// #3/4 trigger ChartControl to repaint candles with new positions and bid/ask lines
			// ALREADY_HANDLED_BY_chartControl_BarAddedUpdated_ShouldTriggerRepaint
			//if (this.ChartFormManager.ContextCurrentChartOrStrategy.IsStreaming) {
			//	chartFormSafe.ChartControl.InvalidateAllPanels();
			//}

			// MOVED_TO_ScriptExecutor_USING_RaiseOpenPositionsUpdatedDueToStreamingNewQuote_step2of3() #4/4 notify Positions that it should update open positions, I wanna see current profit/loss and relevant red/green background
			//List<Position> positionsOpenNowSafeCopy = executorSafe.ExecutionDataSnapshot.PositionsOpenNowSafeCopy;
			//if (positionsOpenNowSafeCopy.Count > 0) {
			//	this.ChartFormManager.ReportersFormsManager.UpdateOpenPositionsDueToStreamingNewQuote_step2of3(positionsOpenNowSafeCopy);
			//}
		}
		#endregion

		public override string ToString() {
			var symbolSafe			= this.Symbol_nullReported;
			var chartShadowSafe		= this.ChartShadow_nullReported;
			var scaleIntervalSafe	= this.ScaleInterval_nullReported;
			string ret = "ChartShadow.Symbol[" + symbolSafe + "](" + scaleIntervalSafe + ")";

			//HANGS_ON_STARTUP__#D_STACK_IS_BLANK__VS2010_HINTED_IM_ACCESSING_this.ChartForm.Text_FROM_DDE_QUOTE_GENERATOR (!?!?!)
			if (chartShadowSafe.InvokeRequired == false) {
				ret += " CHART.TEXT[" + chartShadowSafe.Text + "]";
			} else {
//				ChartFormDataSnapshot snap = this.chartFormManager.DataSnapshot;
//				if (snap == null) {
//					Assembler.PopupException(null);
//				}
//				ContextChart ctx = this.chartFormManager.DataSnapshot.ContextChart;
				ret += (this.Executor_nullReported.Strategy != null)
					? " ScriptContextCurrent[" + this.Executor_nullReported.Strategy.ScriptContextCurrent.ToString() + "]"
					//: " ContextChart[" + this.chartFormManager.DataSnapshot.ContextChart.ToString() + "]"
					: " ContextChart[UNACCESSIBLE]"
					;
			}

			return "{" + ret + "}";
		}
	}
}