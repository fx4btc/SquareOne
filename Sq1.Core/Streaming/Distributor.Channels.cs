﻿using System;
using System.Collections.Generic;

using Sq1.Core.DataTypes;
using Sq1.Core.Charting;
using Sq1.Core.Backtesting;
using Sq1.Core.Livesim;

namespace Sq1.Core.Streaming {
	public partial class Distributor<STREAMING_CONSUMER_CHILD> {
		public		Dictionary<string, SymbolChannel<STREAMING_CONSUMER_CHILD>>	ChannelsBySymbol	{ get; protected set; }

		public virtual bool ConsumerQuoteSubscribe_solidifiers(string symbol, BarScaleInterval scaleInterval,
							STREAMING_CONSUMER_CHILD quoteConsumer, bool quotePumpSeparatePushingThreadEnabled) { lock (this.lockConsumersBySymbol) {
			if (this.ChannelsBySymbol.ContainsKey(symbol) == false) {
				SymbolChannel<STREAMING_CONSUMER_CHILD> newChannel = new SymbolChannel<STREAMING_CONSUMER_CHILD>(this, symbol, quotePumpSeparatePushingThreadEnabled, this.ReasonIwasCreated);
				this.ChannelsBySymbol.Add(symbol, newChannel);
			}
			SymbolChannel<STREAMING_CONSUMER_CHILD> symbolChannel = this.ChannelsBySymbol[symbol];
			// second-deserialized: chartNoStrategy on RIM3_20-minutes => Pump/Thread should be started as well
			if (symbolChannel.QuotePump_nullUnsafe != null && symbolChannel.QuotePump_nullUnsafe.Paused) symbolChannel.QuotePump_nullUnsafe.PusherUnpause_waitUntilUnpaused();
			if (this.StreamingAdapter.UpstreamIsSubscribed(symbol) == false) {
				this.StreamingAdapter.UpstreamSubscribe(symbol);
			}
			return symbolChannel.ConsumerQuoteAdd(scaleInterval, quoteConsumer);
		} }
		public virtual bool ConsumerQuoteUnsubscribe_solidifiers(string symbol, BarScaleInterval scaleInterval, STREAMING_CONSUMER_CHILD quoteConsumer) { lock (this.lockConsumersBySymbol) {
			if (this.ChannelsBySymbol.ContainsKey(symbol) == false) {
				string msg = "I_REFUSE_TO_REMOVE_UNSUBSCRIBED_SYMBOL symbol[" + symbol + "] for quoteConsumer[" + quoteConsumer + "]";
				Assembler.PopupException(msg);
				return true;
			}
			SymbolChannel<STREAMING_CONSUMER_CHILD> channel = this.ChannelsBySymbol[symbol];
			bool removed = channel.ConsumerQuoteRemove(scaleInterval, quoteConsumer);
			if (channel.ConsumersBarCount == 0 && channel.ConsumersQuoteCount == 0) {
				//Assembler.PopupException("QuoteConsumer [" + consumer + "] was the last one using [" + symbol + "]; removing QuoteBarDistributor[" + channel + "]");
				if (channel.QuotePump_nullUnsafe != null) channel.QuotePump_nullUnsafe.PushingThreadStop_waitConfirmed();
				this.ChannelsBySymbol.Remove(symbol);
				//Assembler.PopupException("...UpstreamUnSubscribing [" + symbol + "]");
				this.StreamingAdapter.UpstreamUnSubscribe(symbol);
				return true;
			}
			return false;
		} }
		public virtual bool ConsumerQuoteIsSubscribed_solidifiers(string symbol, BarScaleInterval scaleInterval, STREAMING_CONSUMER_CHILD quoteConsumer) {
			if (this.ChannelsBySymbol.ContainsKey(symbol) == false) {
				//string msg = "I_REFUSE_TO_CHECK_UNSUBSCRIBED_SYMBOL symbol[" + symbol + "] for quoteConsumer[" + quoteConsumer + "]";
				//Assembler.PopupException(msg);
				return false;
			}
			SymbolChannel<STREAMING_CONSUMER_CHILD> channel = this.ChannelsBySymbol[symbol];
			bool subscribed = channel.ConsumerQuoteIsSubscribed(scaleInterval, quoteConsumer);
			return subscribed;
		}


		#region USE_THESE_RAILS
		public virtual bool ConsumerQuoteSubscribe(STREAMING_CONSUMER_CHILD quoteConsumer, bool quotePumpSeparatePushingThreadEnabled) {
		    return this.ConsumerQuoteSubscribe_solidifiers(quoteConsumer.Symbol, quoteConsumer.ScaleInterval, quoteConsumer, quotePumpSeparatePushingThreadEnabled);
		}
		public virtual bool ConsumerQuoteUnsubscribe(STREAMING_CONSUMER_CHILD quoteConsumer) {
		    return this.ConsumerQuoteUnsubscribe_solidifiers(quoteConsumer.Symbol, quoteConsumer.ScaleInterval, quoteConsumer);
		}
		public virtual bool ConsumerQuoteIsSubscribed(STREAMING_CONSUMER_CHILD quoteConsumer) {
		    return this.ConsumerQuoteIsSubscribed_solidifiers(quoteConsumer.Symbol, quoteConsumer.ScaleInterval, quoteConsumer);
		}

		public virtual bool ConsumerBarSubscribe(STREAMING_CONSUMER_CHILD barConsumer, bool barPumpSeparatePushingThreadEnabled) {
		    return this.ConsumerBarSubscribe_solidifiers(barConsumer.Symbol, barConsumer.ScaleInterval, barConsumer, barPumpSeparatePushingThreadEnabled);
		}
		public virtual bool ConsumerBarUnsubscribe(STREAMING_CONSUMER_CHILD barConsumer) {
		    return this.ConsumerBarUnsubscribe_solidifiers(barConsumer.Symbol, barConsumer.ScaleInterval, barConsumer);
		}
		public virtual bool ConsumerBarIsSubscribed(STREAMING_CONSUMER_CHILD barConsumer) {
		    return this.ConsumerBarIsSubscribed_solidifiers(barConsumer.Symbol, barConsumer.ScaleInterval, barConsumer);
		}
		#endregion

		public virtual bool ConsumerBarSubscribe_solidifiers(string symbol, BarScaleInterval scaleInterval,
							STREAMING_CONSUMER_CHILD barConsumer, bool quotePumpSeparatePushingThreadEnabled) { lock (this.lockConsumersBySymbol) {
			if (barConsumer is StreamingConsumerSolidifier) {
				string msg = "StreamingSolidifier_DOESNT_SUPPORT_ConsumerBarsToAppendInto";
			} else {
				Bar barStaticLast = barConsumer.ConsumerBars_toAppendInto.BarStaticLast_nullUnsafe;
				bool isLive				= barConsumer			is StreamingConsumerChart;
				bool isBacktest			= barConsumer			is BacktestStreamingConsumer;
				bool isLivesim			= barConsumer			is LivesimStreamingConsumer;
				bool isLivesimDefault	= this.StreamingAdapter is LivesimStreamingDefault;
				if (barStaticLast == null) {
					if (isLivesimDefault == false) {	// isBacktest,isLivesim are magically fine; where did you notice the problem?
						string msg = "YOUR_BAR_CONSUMER_SHOULD_HAVE_BarStaticLast_NON_NULL"
							+ " MOST_LIKELY_YOU_WILL_GET_MESSAGE__THERE_IS_NO_STATIC_BAR_DURING_FIRST_4_QUOTES_GENERATED__ONLY_STREAMING";
						Assembler.PopupException(msg, null, false);
					}
				}
			}
			if (this.ChannelsBySymbol.ContainsKey(symbol) == false) {
				SymbolChannel<STREAMING_CONSUMER_CHILD> newChannel = new SymbolChannel<STREAMING_CONSUMER_CHILD>(this, symbol, quotePumpSeparatePushingThreadEnabled, this.ReasonIwasCreated);
				this.ChannelsBySymbol.Add(symbol, newChannel);
			}
			SymbolChannel<STREAMING_CONSUMER_CHILD> symbolChannel = this.ChannelsBySymbol[symbol];
			// first-deserialized: Strategy on RIM3_5-minutes => Pump/Thread should be started as well
			if (symbolChannel.QuotePump_nullUnsafe != null && symbolChannel.QuotePump_nullUnsafe.Paused) symbolChannel.QuotePump_nullUnsafe.PusherUnpause_waitUntilUnpaused();
			if (this.StreamingAdapter.UpstreamIsSubscribed(symbol) == false) {
				this.StreamingAdapter.UpstreamSubscribe(symbol);
			}
			return symbolChannel.ConsumerBarAdd(scaleInterval, barConsumer);
		} }
		public virtual bool ConsumerBarUnsubscribe_solidifiers(string symbol, BarScaleInterval scaleInterval,
										STREAMING_CONSUMER_CHILD barConsumer) { lock (this.lockConsumersBySymbol) {
			if (this.ChannelsBySymbol.ContainsKey(symbol) == false) {
				string msg = "I_REFUSE_TO_REMOVE_UNSUBSCRIBED_SYMBOL symbol[" + symbol + "] barConsumer[" + barConsumer + "]";
				Assembler.PopupException(msg);
				return false;
			}
			SymbolChannel<STREAMING_CONSUMER_CHILD> channel = this.ChannelsBySymbol[symbol];
			bool removed = channel.ConsumerBarRemove(scaleInterval, barConsumer);
			if (channel.ConsumersBarCount == 0 && channel.ConsumersQuoteCount == 0) {
				//Assembler.PopupException("BarConsumer [" + consumer + "] was the last one using [" + symbol + "]; removing QuoteBarDistributor[" + distributor + "]");
				if (channel.QuotePump_nullUnsafe != null) channel.QuotePump_nullUnsafe.PushingThreadStop_waitConfirmed();
				//Assembler.PopupException("BarConsumer [" + scaleInterval + "] was the last one listening for [" + symbol + "]");
				//Assembler.PopupException("...removing[" + symbol + "] from this.ChannelsBySymbol[" + this.ChannelsBySymbol + "]");
				this.ChannelsBySymbol.Remove(symbol);
				//Assembler.PopupException("...UpstreamUnSubscribing [" + symbol + "]");
				this.StreamingAdapter.UpstreamUnSubscribe(symbol);
				return true;
			}
			return false;
		} }
		public virtual bool ConsumerBarIsSubscribed_solidifiers(string symbol, BarScaleInterval scaleInterval,
										STREAMING_CONSUMER_CHILD barConsumer) {
			if (this.ChannelsBySymbol.ContainsKey(symbol) == false) {
				//string msg = "I_REFUSE_TO_CHECK_UNSUBSCRIBED_SYMBOL symbol[" + symbol + "] for barConsumer[" + barConsumer + "]";
				//Assembler.PopupException(msg);
				return false;
			}
			SymbolChannel<STREAMING_CONSUMER_CHILD> channel = this.ChannelsBySymbol[symbol];
			bool subscribed = channel.ConsumerBarIsSubscribed(scaleInterval, barConsumer);
			return subscribed;
		}


		public SymbolChannel<STREAMING_CONSUMER_CHILD> GetChannelFor_nullMeansWasntSubscribed(string symbol) {
			if (this.ChannelsBySymbol.ContainsKey(symbol) == false) {
				string msg = "LIVESIM_WITH_OWN_IMPLEMENTATION_SHOULD_HAVE_BEEN_SUBSCRIBED_TO_LIVESIMMING_BARS"
					+ " YOU_REQUESTED_CHANNEL_THAT_YOU_DIDNT_TELL_ME_TO_CREATE";
				Assembler.PopupException(msg, null, false);
				return null;
			}
			SymbolChannel<STREAMING_CONSUMER_CHILD> ret = this.ChannelsBySymbol[symbol];
			return ret;
		}
		public List<SymbolScaleStream<STREAMING_CONSUMER_CHILD>> GetStreams_allScaleIntervals_forSymbol(string symbol) { lock (this.lockConsumersBySymbol) {
			List<SymbolScaleStream<STREAMING_CONSUMER_CHILD>> streams = new List<SymbolScaleStream<STREAMING_CONSUMER_CHILD>>();
			if (this.ChannelsBySymbol.ContainsKey(symbol) == false) {
				string msg = "STARTING_LIVESIM:CLICK_CHART>BARS>SUBSCRIBE symbol[" + symbol + "]"
					//+ " YOU_DIDNT_SUBSCRIBE_AFTER_DISTRIBUTION_CHANNELS_CLEAR"
					//+ " MOST_LIKELY_YOU_ABORTED_BACKTEST_BY_CHANGING_SELECTORS_IN_GUI_FIX_HANDLERS"
					;
				//Assembler.PopupException(msg, null, false);
				return streams;
			}
			SymbolChannel<STREAMING_CONSUMER_CHILD> channel = this.ChannelsBySymbol[symbol];
			return channel.AllStreams_safeCopy;
		} }
		public SymbolScaleStream<STREAMING_CONSUMER_CHILD> GetStreamFor_nullUnsafe(string symbol, BarScaleInterval barScaleInterval) { lock (this.lockConsumersBySymbol) {
			if (this.ChannelsBySymbol.ContainsKey(symbol) == false) {
				string msg = "NO_SYMBOL_SUBSCRIBED Distributor[" + this + "].ChannelsBySymbol.ContainsKey(" + symbol + ")=false INVOKER_NULL_CHECK_EYEBALLED";
				//Assembler.PopupException(msg, null, false);
				return null;
			}
			SymbolChannel<STREAMING_CONSUMER_CHILD> channel = this.ChannelsBySymbol[symbol];
			if (channel.StreamsByScaleInterval.ContainsKey(barScaleInterval) == false) {
				string msg = "NO_SCALEINTERVAL_SUBSCRIBED Distributor[" + this
					+ "].ChannelsBySymbol[" + symbol + "].ContainsKey(" + barScaleInterval + ")=false";
				Assembler.PopupException(msg);
				return null;
			}
			return channel.StreamsByScaleInterval[barScaleInterval];
		} }
		public List<SymbolScaleStream<STREAMING_CONSUMER_CHILD>> GetStreams_forSymbol_exceptForChartLivesimming(string symbol
					, BarScaleInterval scaleIntervalOnly_anyIfNull, STREAMING_CONSUMER_CHILD chartShadowToExclude) { lock (this.lockConsumersBySymbol) {
			List<SymbolScaleStream<STREAMING_CONSUMER_CHILD>> ret = new List<SymbolScaleStream<STREAMING_CONSUMER_CHILD>>();
			if (this.ChannelsBySymbol.ContainsKey(symbol) == false) {
				string msg = "YOU_DIDNT_SUBSCRIBE_AFTER_DISTRIBUTION_CHANNELS_CLEAR symbol[" + symbol + "] MOST_LIKELY_YOU_ABORTED_BACKTEST_BY_CHANGING_SELECTORS_IN_GUI_FIX_HANDLERS";
				Assembler.PopupException(msg, null, false);
				return null;
			}
			SymbolChannel<STREAMING_CONSUMER_CHILD> channel = this.ChannelsBySymbol[symbol];
			if (scaleIntervalOnly_anyIfNull != null && channel.StreamsByScaleInterval.ContainsKey(scaleIntervalOnly_anyIfNull) == false) {
				string msg = "NO_SCALEINTERVAL_SUBSCRIBED Distributor[" + this
					+ "].ChannelsBySymbol[" + symbol + "].ContainsKey(" + scaleIntervalOnly_anyIfNull + ")=false";
				Assembler.PopupException(msg);
				return null;
			}
			foreach (SymbolScaleStream<STREAMING_CONSUMER_CHILD> stream in channel.AllStreams_safeCopy) {
				if (scaleIntervalOnly_anyIfNull != null && stream.ScaleInterval != scaleIntervalOnly_anyIfNull) continue;
				SymbolScaleStream<STREAMING_CONSUMER_CHILD> channelClone = stream.CloneFullyFunctional_withNewDictioniariesAndLists_toPossiblyRemoveMatchingConsumers();
				if (chartShadowToExclude != null) {
					if (channelClone.ConsumersBarContains	(chartShadowToExclude)) channelClone.ConsumerBarRemove		(chartShadowToExclude);
					if (channelClone.ConsumersQuoteContains	(chartShadowToExclude)) channelClone.ConsumerQuoteRemove	(chartShadowToExclude);
				}
				if (channelClone.ConsumersBarCount == 0 && channelClone.ConsumersQuoteCount == 0) continue;
				ret.Add(channelClone);
			}
			return ret;
		} }

		internal void SetQuotePumpThreadName_sinceNoMoreSubscribersWillFollowFor(string symbol) {
			if (this.ChannelsBySymbol.ContainsKey(symbol) == false) {
				string msg = "NO_SYMBOL_SUBSCRIBED Distributor[" + this + "].ChannelsBySymbol.ContainsKey(" + symbol + ")=false";
				Assembler.PopupException(msg, null, false);
				return;
			}
			SymbolChannel<STREAMING_CONSUMER_CHILD> channel = this.ChannelsBySymbol[symbol];
			if (channel == null) {
				string msg = "SPLIT_QUOTE_PUMP_TO_SINGLE_THREADED_AND_SELF_LAUNCHING";
				Assembler.PopupException(msg);
				return;
			}
			if (this.StreamingAdapter.QuotePumpSeparatePushingThreadEnabled) {
				channel.QueueWhenBacktesting_PumpForLiveAndLivesim.UpdateThreadNameAfterMaxConsumersSubscribed = true;
				// SELF_MANAGED_BY_CHANNEL channel.QuoteQueue.PusherUnpause();
				//DONT_SET_GUI_THREAD_NAME__FLAG_IS_SET_WILL_SET_THREAD_NAME_FROM_WITHIN channel.QuoteQueue_onlyWhenBacktesting_quotePumpForLiveAndSim.SetThreadName();
			}
		}


		//internal void ForceUnsubscribeLeftovers_mustBeEmptyAlready(string newReasonToExist) {
		//    string ret = "";
		//    //v1 - doesn't dispose Pump's MRE => Handles leak this.ChannelsBySymbol.Clear();

		//    foreach (string symbol in this.ChannelsBySymbol.Keys) {
		//        string perSymbol = "symbol[" + symbol + "]";
		//        Dictionary<BarScaleInterval, SymbolScaleStream<STREAMING_CONSUMER_CHILD>> channelsForEachSymbol = new Dictionary<BarScaleInterval, SymbolScaleStream<STREAMING_CONSUMER_CHILD>>(this.ChannelsBySymbol[symbol]);
		//        foreach(SymbolScaleStream<STREAMING_CONSUMER_CHILD> eachChannel in new List<SymbolScaleStream<STREAMING_CONSUMER_CHILD>>(channelsForEachSymbol.Values)) {
		//            string perScaleInterval = "scaleInterval[" + eachChannel.ScaleInterval + "]";

		//            string barConsumersUnsubscribed = "";
		//            foreach (StreamingConsumer eachBarConsumer in eachChannel.ConsumersBar) {
		//                if (barConsumersUnsubscribed != "") barConsumersUnsubscribed += ",";
		//                barConsumersUnsubscribed += "[" + eachBarConsumer + "]";
		//                this.ConsumerBarUnsubscribe(symbol, eachChannel.ScaleInterval, eachBarConsumer);	// affects original List, our new List<>() keeps enumerating
		//            }
		//            if (eachChannel.ConsumersBarCount > 0) {
		//                string msg = "MUST_BE_EMPTY__BY_NOW eachChannel[" + eachChannel.ToString() + "].ConsumersBarCount[" + eachChannel.ConsumersBarCount + "]";
		//                Assembler.PopupException(msg);
		//            }

		//            string quoteConsumersUnsubscribed = "";
		//            foreach (StreamingConsumer eachQuoteConsumer in eachChannel.ConsumersQuote) {
		//                if (quoteConsumersUnsubscribed != "") quoteConsumersUnsubscribed += ",";
		//                quoteConsumersUnsubscribed += "[" + eachQuoteConsumer + "]";
		//                this.ConsumerQuoteUnsubscribe(symbol, eachChannel.ScaleInterval, eachQuoteConsumer);	// affects original List, our new List<>() keeps enumerating
		//            }
		//            if (eachChannel.ConsumersQuoteCount > 0) {
		//                string msg = "MUST_BE_EMPTY__BY_NOW eachChannel[" + eachChannel.ToString() + "].ConsumersQuoteCount[" + eachChannel.ConsumersQuoteCount + "]";
		//                Assembler.PopupException(msg);
		//            }

		//            eachChannel.QuotePump_nullUnsafe.DisposeAllHandles();
		//            perSymbol += perScaleInterval + ":barConsumers{" + barConsumersUnsubscribed + "},quoteConsumers{" + quoteConsumersUnsubscribed + "}";
		//        }
		//        ret += " " + perSymbol;
		//    }
		//    if (ret != "") {
		//        string msig = " //ForceUnsubscribeLeftovers(newReasonToExist[" + newReasonToExist + "])";
		//        string msg = "LIVESIM_DISTRIBUTOR_MUST_HAVE_BEEN_CLEAN_BEFORE_REUSING DISTRIBUTOR_FOR_DUMMY_STREAMING_MUST_BE_EMPTY";
		//        Assembler.PopupException(msg + ret + msig);
		//    }
		//    this.ReasonIwasCreated = newReasonToExist;
		//}
	}
}