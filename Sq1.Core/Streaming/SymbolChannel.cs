﻿using System;
using System.Collections.Generic;

using Sq1.Core.DataTypes;
using Sq1.Core.Backtesting;
using Sq1.Core.Livesim;
using Sq1.Core.Charting;

namespace Sq1.Core.Streaming {
	public class SymbolChannel<STREAMING_CONSUMER_CHILD> : IDisposable
						 where STREAMING_CONSUMER_CHILD : StreamingConsumer {
				string					symbol;
		public	string					ReasonIwasCreated_propagatedFromDistributor		{ get; private set; }
		public	Distributor<STREAMING_CONSUMER_CHILD>				Distributor										{ get; private set; }

				object					lockStreamsDictionary;

		public	QueuePerSymbol<Quote, STREAMING_CONSUMER_CHILD>	QueueWhenBacktesting_PumpForLiveAndLivesim		{ get; protected set; }
		public	PumpPerSymbol<Quote, STREAMING_CONSUMER_CHILD>	QuotePump_nullUnsafe							{ get { return this.QueueWhenBacktesting_PumpForLiveAndLivesim as PumpPerSymbol<Quote, STREAMING_CONSUMER_CHILD>; } }
		public	bool					ImQueueNotPump_trueOnlyForBacktest				{ get { return this.QuotePump_nullUnsafe == null; } }
				bool					pump_separatePushingThread_enabled;

		public	Dictionary<BarScaleInterval, SymbolScaleStream<STREAMING_CONSUMER_CHILD>>	StreamsByScaleInterval	{ get; protected set; }
		public	List<SymbolScaleStream<STREAMING_CONSUMER_CHILD>>							AllStreams_safeCopy		{ get { lock (this.lockStreamsDictionary) {
			return new List<SymbolScaleStream<STREAMING_CONSUMER_CHILD>>(this.StreamsByScaleInterval.Values);
		} } }

		~SymbolChannel() { this.Dispose(); }
		SymbolChannel() {
			lockStreamsDictionary					= new object();
			backtestersRunning_causingPumpingPause	= new List<Backtester>();
			StreamsByScaleInterval					= new Dictionary<BarScaleInterval, SymbolScaleStream<STREAMING_CONSUMER_CHILD>>();
		}

		public SymbolChannel(Distributor<STREAMING_CONSUMER_CHILD> dataDistributor, string symbol,
							bool quotePumpSeparatePushingThreadEnabled, string reasonIwasCreated) : this() {
			this.Distributor = dataDistributor;
			this.symbol = symbol;
			this.pump_separatePushingThread_enabled = quotePumpSeparatePushingThreadEnabled;

			if (string.IsNullOrEmpty(reasonIwasCreated)) {
				string msg = "WITHOUT_reasonIwasCreated_PUMP_AND_STREAMS_WILL_THROW";
				Assembler.PopupException(msg);
			}
			this.ReasonIwasCreated_propagatedFromDistributor = reasonIwasCreated;

			if (quotePumpSeparatePushingThreadEnabled) {
				this.QueueWhenBacktesting_PumpForLiveAndLivesim = new PumpPerSymbol<Quote, STREAMING_CONSUMER_CHILD>(this);
			} else {
				this.QueueWhenBacktesting_PumpForLiveAndLivesim = new QueuePerSymbol<Quote, STREAMING_CONSUMER_CHILD>(this);
			}
		}


		public void PushQuote_toStreams(Quote quoteDequeued_singleInstance_tillStreamBindsAll) {
			foreach (SymbolScaleStream<STREAMING_CONSUMER_CHILD> stream in this.StreamsByScaleInterval.Values)  {
				try {
					stream.PushQuote_toConsumers(quoteDequeued_singleInstance_tillStreamBindsAll);
				} catch (Exception ex) {
					string msg = "STREAM_THREW [" + stream + "] WHILE_PUSHING quoteDequeued_singleInstance_tillStreamBindsAll[" + quoteDequeued_singleInstance_tillStreamBindsAll + "]";
					Assembler.PopupException(msg, ex);
				}
			}
		}

		public void PushQuote_viaPumpOrQueue(Quote quoteDequeued_singleInstance_tillStreamBindsAll) {
			Quote quote = quoteDequeued_singleInstance_tillStreamBindsAll;
			if (string.IsNullOrEmpty(quote.Symbol)) {
				Assembler.PopupException("quote.Symbol[" + quote.Symbol + "] is null or empty, returning");
				return;
			}
			if (quote.Symbol != this.symbol) {
				Assembler.PopupException("quote.Symbol[" + quote.Symbol + "] != this.Symbol[" + this.symbol + "], returning");
				return;
			}

			//v1 this.PushQuoteToConsumers(quoteSernoEnrichedWithUnboundStreamingBar);
			//v2 let the user re-backtest during live streaming using 1) QuotePump.OnHold=true; 2) RunBacktest(); 3) QuotePump.OnHold=false;
			int straightOrBuffered = this.QueueWhenBacktesting_PumpForLiveAndLivesim.Push_straightOrBuffered(quote);
			if (this.QueueWhenBacktesting_PumpForLiveAndLivesim.HasSeparatePushingThread) {
				int pushedBuffered = straightOrBuffered;
			} else {
				int pushedStraight = straightOrBuffered;
			}
		}

		public bool ConsumerQuoteAdd(BarScaleInterval scaleInterval, STREAMING_CONSUMER_CHILD quoteConsumer) { lock (this.lockStreamsDictionary) {
			if (this.StreamsByScaleInterval.ContainsKey(scaleInterval) == false) {
				//v1 CAN_NOT_CREATE_INSTANCE_OF_AN_ABSTRACT SymbolScaleStream<STREAMING_CONSUMER_CHILD> newScaleChannel = new SymbolScaleStream<STREAMING_CONSUMER_CHILD>(this, symbol, scaleInterval, this.ReasonIwasCreated_propagatedFromDistributor);
				SymbolScaleStream<STREAMING_CONSUMER_CHILD> newScaleChannel = streamingConsumer_factory(scaleInterval, quoteConsumer);
				this.StreamsByScaleInterval.Add(scaleInterval, newScaleChannel);
			}
			SymbolScaleStream<STREAMING_CONSUMER_CHILD> stream = this.StreamsByScaleInterval[scaleInterval];
			return stream.ConsumerQuoteAdd(quoteConsumer);
		} }

		SymbolScaleStream<STREAMING_CONSUMER_CHILD> streamingConsumer_factory(BarScaleInterval scaleInterval, STREAMING_CONSUMER_CHILD quoteConsumer) {
			SymbolScaleStream<STREAMING_CONSUMER_CHILD> newScaleChannel = null;	// default(SymbolScaleStream<STREAMING_CONSUMER_CHILD>);
			if (quoteConsumer.GetType().IsInstanceOfType(typeof(StreamingConsumerChart))) {
				SymbolChannel<StreamingConsumerChart> myself = this as SymbolChannel<StreamingConsumerChart>;
				SymbolScaleStream<StreamingConsumerChart> concrete = new SymbolScaleStreamChart(myself, symbol, scaleInterval, this.ReasonIwasCreated_propagatedFromDistributor);
				newScaleChannel = concrete as SymbolScaleStream<STREAMING_CONSUMER_CHILD>;
			} else if (quoteConsumer.GetType().IsInstanceOfType(typeof(StreamingConsumerSolidifier))) {
				SymbolChannel<StreamingConsumerSolidifier> myself = this as SymbolChannel<StreamingConsumerSolidifier>;
				SymbolScaleStream<StreamingConsumerSolidifier> concrete = new SymbolScaleStreamSolidifier(myself, symbol, scaleInterval, this.ReasonIwasCreated_propagatedFromDistributor);
				newScaleChannel = concrete as SymbolScaleStream<STREAMING_CONSUMER_CHILD>;
			} else {
				throw new Exception("YOU_ADDED_NEW_CHILD_OF_StreamingConsumer_BUT_DIDNT_IMPLEMENT_CASE_INDIDE_ConsumerQuoteAdd()");
			}
			return newScaleChannel;
		}

		public bool ConsumerQuoteRemove(BarScaleInterval scaleInterval, STREAMING_CONSUMER_CHILD quoteConsumer) { lock (this.lockStreamsDictionary) {
			if (this.StreamsByScaleInterval.ContainsKey(scaleInterval) == false) {
				string msg = "I_REFUSE_TO_REMOVE__SCALE_INTERVAL_NOT_SUBSCRIBED [" + scaleInterval + "] for [" + quoteConsumer + "]"
					+ " NOT_FOUND_AMONG_[" + this.ConsumersQuoteAsString + "]";
				Assembler.PopupException(msg);
				return false;
			}
			SymbolScaleStream<STREAMING_CONSUMER_CHILD> stream = this.StreamsByScaleInterval[scaleInterval];
			bool removed = stream.ConsumerQuoteRemove(quoteConsumer);
			if (stream.ConsumersBarCount == 0 && stream.ConsumersQuoteCount == 0) {
				//Assembler.PopupException("QuoteConsumer [" + consumer + "] was the last one using [" + scaleInterval + "]; removing StreamsByScaleInterval[" + channel + "]");
				this.StreamsByScaleInterval.Remove(scaleInterval);
			}
			return removed;
		} }

		public bool ConsumerQuoteIsSubscribed(BarScaleInterval scaleInterval, STREAMING_CONSUMER_CHILD quoteConsumer) { lock (this.lockStreamsDictionary) {
			bool ret = false;
			if (this.StreamsByScaleInterval.ContainsKey(scaleInterval) == false) {
				string msg = "I_REFUSE_TO_CHECK_SUBSCRIBED__SCALE_INTERVAL_NOT_SUBSCRIBED [" + scaleInterval + "] for [" + quoteConsumer + "]"
					+ " NOT_FOUND_AMONG_[" + this.ConsumersQuoteAsString + "]";
				Assembler.PopupException(msg);
				return false;
			}
			SymbolScaleStream<STREAMING_CONSUMER_CHILD> stream = this.StreamsByScaleInterval[scaleInterval];
			ret = stream.ConsumersQuoteContains(quoteConsumer);
			return ret;
		} }

		public bool ConsumerQuoteIsSubscribed(STREAMING_CONSUMER_CHILD quoteConsumer) { lock (this.lockStreamsDictionary) {
			bool ret = false;
			foreach (BarScaleInterval scaleInterval in this.StreamsByScaleInterval.Keys)  {
				ret &= this.ConsumerQuoteIsSubscribed(scaleInterval, quoteConsumer);
			}
			return ret;
		} }



		public bool ConsumerBarAdd(BarScaleInterval scaleInterval, STREAMING_CONSUMER_CHILD barConsumer) { lock (this.lockStreamsDictionary) {
			if (this.StreamsByScaleInterval.ContainsKey(scaleInterval) == false) {
				//v1 SymbolScaleStream<STREAMING_CONSUMER_CHILD> newScaleChannel = new SymbolScaleStream<STREAMING_CONSUMER_CHILD>(this, symbol, scaleInterval, this.ReasonIwasCreated_propagatedFromDistributor);
				SymbolScaleStream<STREAMING_CONSUMER_CHILD> newScaleChannel = streamingConsumer_factory(scaleInterval, barConsumer);
				this.StreamsByScaleInterval.Add(scaleInterval, newScaleChannel);
			}
			SymbolScaleStream<STREAMING_CONSUMER_CHILD> stream = this.StreamsByScaleInterval[scaleInterval];
			return stream.ConsumerBarAdd(barConsumer);
		} }

		public bool ConsumerBarRemove(BarScaleInterval scaleInterval, STREAMING_CONSUMER_CHILD barConsumer) { lock (this.lockStreamsDictionary) {
			if (this.StreamsByScaleInterval.ContainsKey(scaleInterval) == false) {
				string msg = "I_REFUSE_TO_REMOVE__SCALE_INTERVAL_NOT_SUBSCRIBED [" + scaleInterval + "] for [" + barConsumer + "]"
					+ " NOT_FOUND_AMONG_[" + this.ConsumersBarAsString + "]";
				Assembler.PopupException(msg);
				return false;
			}
			SymbolScaleStream<STREAMING_CONSUMER_CHILD> stream = this.StreamsByScaleInterval[scaleInterval];
			bool removed = stream.ConsumerBarRemove(barConsumer);
			if (stream.ConsumersBarCount == 0 && stream.ConsumersBarCount == 0) {
				//Assembler.PopupException("BarConsumer [" + consumer + "] was the last one using [" + scaleInterval + "]; removing StreamsByScaleInterval[" + channel + "]");
				this.StreamsByScaleInterval.Remove(scaleInterval);
			}
			return removed;
		} }

		public bool ConsumerBarIsSubscribed(BarScaleInterval scaleInterval, STREAMING_CONSUMER_CHILD barConsumer) { lock (this.lockStreamsDictionary) {
			bool ret = false;
			if (this.StreamsByScaleInterval.ContainsKey(scaleInterval) == false) {
				string msg = "I_REFUSE_TO_CHECK_SUBSCRIBED__SCALE_INTERVAL_NOT_SUBSCRIBED [" + scaleInterval + "] for [" + barConsumer + "]"
					+ " NOT_FOUND_AMONG_[" + this.ConsumersBarAsString + "]";
				Assembler.PopupException(msg);
				return false;
			}
			SymbolScaleStream<STREAMING_CONSUMER_CHILD> stream = this.StreamsByScaleInterval[scaleInterval];
			ret = stream.ConsumersBarContains(barConsumer);
			return ret;
		} }

		public bool ConsumerBarIsSubscribed(STREAMING_CONSUMER_CHILD barConsumer) { lock (this.lockStreamsDictionary) {
			bool ret = false;
			foreach (BarScaleInterval scaleInterval in this.StreamsByScaleInterval.Keys)  {
				ret &= this.ConsumerBarIsSubscribed(scaleInterval, barConsumer);
			}
			return ret;
		} }



		public int ConsumersBarCount { get { lock (this.lockStreamsDictionary) {
			int ret = 0;
			foreach (SymbolScaleStream<STREAMING_CONSUMER_CHILD> stream in this.StreamsByScaleInterval.Values)  {
				ret += stream.ConsumersBarCount;
			}
			return ret;
		} } }
		public int ConsumersQuoteCount { get { lock (this.lockStreamsDictionary) {
			int ret = 0;
			foreach (SymbolScaleStream<STREAMING_CONSUMER_CHILD> stream in this.StreamsByScaleInterval.Values)  {
				ret += stream.ConsumersQuoteCount;
			}
			return ret;
		} } }


		public List<STREAMING_CONSUMER_CHILD> ConsumersBar { get { lock (this.lockStreamsDictionary) {
			List<STREAMING_CONSUMER_CHILD> ret = new List<STREAMING_CONSUMER_CHILD>();
			foreach (SymbolScaleStream<STREAMING_CONSUMER_CHILD> stream in this.StreamsByScaleInterval.Values)  {
				ret.AddRange(stream.ConsumersBar);
			}
			return ret;
		} } }

		public List<STREAMING_CONSUMER_CHILD> ConsumersQuote { get { lock (this.lockStreamsDictionary) {
			List<STREAMING_CONSUMER_CHILD> ret = new List<STREAMING_CONSUMER_CHILD>();
			foreach (SymbolScaleStream<STREAMING_CONSUMER_CHILD> stream in this.StreamsByScaleInterval.Values)  {
				ret.AddRange(stream.ConsumersQuote);
			}
			return ret;
		} } }
		public List<STREAMING_CONSUMER_CHILD> Consumers { get { lock (this.lockStreamsDictionary) {
			List<STREAMING_CONSUMER_CHILD> ret = new List<STREAMING_CONSUMER_CHILD>(this.ConsumersQuote);
			foreach (STREAMING_CONSUMER_CHILD consumer in this.ConsumersBar) {
				if (ret.Contains(consumer)) continue;
				ret.Add(consumer);
			}
			return ret;
		} } }


		public string ConsumersQuoteAsString { get { lock (this.lockStreamsDictionary) {
			string ret = "";
			foreach (STREAMING_CONSUMER_CHILD consumer in this.ConsumersQuote) {
				if (ret != "") ret += ", ";
				ret += consumer.ToString();
			}
			return ret;
		} } }
		public string ConsumersBarAsString { get { lock (this.lockStreamsDictionary) {
			string ret = "";
			foreach (STREAMING_CONSUMER_CHILD consumer in this.ConsumersBar) {
				if (ret != "") ret += ", ";
				ret += consumer.ToString();
			}
			return ret;
		} } }



		public string ConsumerNames { get { lock (this.lockStreamsDictionary) {
			return this.ConsumersQuoteNames + this.ConsumersBarNames;
		} } }

		public string ConsumersQuoteNames { get { lock (this.lockStreamsDictionary) {
			string ret = "";
			foreach (STREAMING_CONSUMER_CHILD consumer in this.ConsumersQuote) {
				if (ret != "") ret += ",";
				ret += consumer.ReasonToExist;
			}
			return ret;
		} } }
		public string ConsumersBarNames { get { lock (this.lockStreamsDictionary) {
			string ret = "";
			foreach (STREAMING_CONSUMER_CHILD consumer in this.ConsumersBar) {
				if (ret != "") ret += ",";
				ret += consumer.ReasonToExist;
			}
			return ret;
		} } }

		public string ToString() { lock (this.lockStreamsDictionary) {
			string ret = "";
			foreach (STREAMING_CONSUMER_CHILD consumer in this.Consumers) {
				if (ret != "") ret += ",";
				ret += consumer.ReasonToExist;
			}
			return ret;
		} }



				List<Backtester>			backtestersRunning_causingPumpingPause;
		public void QueueOrPumpPause_addBacktesterLaunchingScript_eachQuote(Backtester backtesterOrLivesimAdding) {	// POTENTINALLY_THREAD_UNSAFE lock(this.lockPump) {
			bool addedFirstBacktester = false;
			// #1/3 add to backtesters running me
			if (this.backtestersRunning_causingPumpingPause.Contains(backtesterOrLivesimAdding)) {
				string msg = "ADD_BACKTESTER_ONLY_ONCE [" + backtesterOrLivesimAdding + "]"
					+ " 1)YOU_INVOKE_Script.OnNewQuote()_WITHOUT_WAITING_FOR_IT_TO_FINISH_PREV_INVOCATION (TRYING_TO_ASSURE_NON_REENTERABILITY_OF_SCRIPT_HOOKS_HERE)"
					+ " 2)Script.OnNewQuote()_THREW_EXCEPTION_AND_YOU_DIDNT_CATCH_IT_AND_DIDNT_REMOVE_BACKTESTER_POSSIBLY_DIDNT_UNPAUSE";
				Assembler.PopupException(msg);
				return;
			} else {
				this.backtestersRunning_causingPumpingPause.Add(backtesterOrLivesimAdding);
				if (this.backtestersRunning_causingPumpingPause.Count == 1) {
					addedFirstBacktester = true;
				}
			}
			// #2/3 livesim => don't pause, just exit
			if (backtesterOrLivesimAdding is Livesimulator) {
				//if (this.ImPumpNotQueue) {
					string msg = "YOU_RUINED_THE_CONCEPT_OF_HAVING_LIVESIM_AS_A_TEST_FOR_LIVE_STREAMING"
						//+ " YOU_SUBSCRIBED_LIVESIM_BARS_IN_SINGLE_THREADED_QUEUE__MUST_BE_A_PUMP_JUST_LIKE_FOR_QUIK_STREAMING"
						+ " SimulationPreBarsSubstitute_overrideable()<=Livesimulator.cs:105"
						+ " ANYWAY_NO_NEED_TO_PAUSE_COMPETITORS NO_COMPETITORS_FOR_LIVESIM_BAR_EVENTS";
					Assembler.PopupException(msg);
				//}
				return;
			}
			// #3/3 it's a backtest => pause
			if (this.QueueWhenBacktesting_PumpForLiveAndLivesim.Paused == true) {
				if (addedFirstBacktester) {
					string msg = "WE_ARE_ON_APP_RESTART_RIGHT??? ALL_PUMPS_ARE_BORN_PAUSED  backtesterAdding=[" + backtesterOrLivesimAdding + "]";
				} else {
					string msg = "PUMP_ALREADY_PAUSED_BY_ANOTHER_CONSUMERS_BACKTEST backtesterAdding=[" + backtesterOrLivesimAdding + "]";
					Assembler.PopupException(msg, null, false);
					//return;
				}
			} else {
				this.QueueWhenBacktesting_PumpForLiveAndLivesim.PusherPause_waitUntilPaused();
			}
		}
		public void QueueOrPumpResume_removeBacktesterFinishedScript_eachQuote(Backtester backtesterOrLivesimRemoving) {
			// #1/3 remove from backtesters running me, because backtest/livesim terminated anyway
			if (this.backtestersRunning_causingPumpingPause.Contains(backtesterOrLivesimRemoving)) {
				this.backtestersRunning_causingPumpingPause.Remove(backtesterOrLivesimRemoving);
			} else {
				string msg = "YOU_NEVER_ADDED_BACKTESTER [" + backtesterOrLivesimRemoving + "]";
				Assembler.PopupException(msg);
				//return;
			}
			// #2/3 livesim => don't unpause, just exit
			if (backtesterOrLivesimRemoving is Livesimulator) {
				//if (this.ImPumpNotQueue) {
					string msg = "YOU_RUINED_THE_CONCEPT_OF_HAVING_LIVESIM_AS_A_TEST_FOR_LIVE_STREAMING"
						//+ " YOU_SUBSCRIBED_LIVESIM_BARS_IN_SINGLE_THREADED_QUEUE__MUST_BE_A_PUMP_JUST_LIKE_FOR_QUIK_STREAMING"
						+ " SimulationPreBarsSubstitute_overrideable()<=Livesimulator.cs:105"
						+ " ANYWAY_NO_NEED_TO_UNPAUSE_COMPETITORS NO_COMPETITORS_FOR_LIVESIM_BAR_EVENTS";
					Assembler.PopupException(msg);
				//}
				return;
			}

			// #3/3 it's a backtest => pause
			if (this.QueueWhenBacktesting_PumpForLiveAndLivesim.Paused == false) {
				string msg = "YOU_RUINED_WHOLE_IDEA_OF_DISTRIBUTOR_CHANNEL_TO_AUTORESUME_ITS_OWN_PUMP backtesterRemoving=[" + backtesterOrLivesimRemoving + "]";
				Assembler.PopupException(msg);
				return;
			}
			if (this.backtestersRunning_causingPumpingPause.Count > 0) {
				string msg = "STILL_HAVE_ANOTHER_BACKTEST_RUNNING_IN_PARALLEL__WILL_UNPAUSE_PUMP_AFTER_LAST_TERMINATES"
					+ " backtestersRunningCausingPumpingPause.Count=[" + this.backtestersRunning_causingPumpingPause.Count + "]"
					+ " after backtesterRemoved[" + backtesterOrLivesimRemoving + "]";
				Assembler.PopupException(msg);
				return;
			}
			this.QueueWhenBacktesting_PumpForLiveAndLivesim.PusherUnpause_waitUntilUnpaused();
		}



		public bool IsDisposed { get; private set; }
		public void Dispose() {
			if (this.IsDisposed) {
				string msg = "ALREADY_DISPOSED__DONT_INVOKE_ME_TWICE__" + this.ToString();
				Assembler.PopupException(msg);
				return;
			}
			if (this.QuotePump_nullUnsafe != null) {
				this.QuotePump_nullUnsafe.Dispose();
			}
			this.IsDisposed = true;
		}
	}
}
