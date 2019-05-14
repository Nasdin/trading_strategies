 #region Using declarations
using System;
using System.Windows.Forms;
using System.Windows.Forms.ComponentModel;
using System.Windows.Forms.Design;
using System.Windows.Forms.Layout;

using System.Globalization;
using System.ComponentModel;

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Indicator;
using NinjaTrader.Strategy;
using System.Collections.Generic;
using System.Collections;
using System.IO; // to enable IO
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
#endregion

namespace NinjaTrader.Strategy{
    [Description("This is a sample multi-time frame strategy.")]
    public class a18092015controlbest : BTStrategy{
		private int Multiplier = 0;
		private long _eFlatTime = 0;
		private long _eStartTime = 0;
		private long _eEndTime = 0;
		private long _StartTime = 0;
		private long _CutOffTime = 0;
		private long _EndTime = 0;
		private int _Multiplier = 0;
		private int _GMT = (int)((TimeSpan)(DateTime.Now - DateTime.Now.ToUniversalTime())).TotalSeconds;
		private int _GMT_MKT = -5 * 3600;
		private double[] bolll = new double[14];
		private double[] bolls = new double[14];
		private double[] longBollAvg = new double[5];
		private double[] shortBollAvg = new double[5];
		private double ourEntryPrice = 0.0;
		private double highestHigh = 0;
		private double lowestLow = 0;
		private string R2signal = "";
		private int r2entrybar = -1;
		private Dictionary<string, double> stoplosstab = new Dictionary<string,double>();	//Maximum stoploss for each contract
		private Dictionary<string, double> marginDebited = new Dictionary<string,double>();	
		private Dictionary<string, double> relativeWeight = new Dictionary<string,double>();	
		private Dictionary<string, VTimings> VTimingDic = new Dictionary<string,VTimings>();	
		private static double marketRiskPercentage = 0.05d;
		public int lookBackPeriod = 35;
		private static HLDO HLCache = null; 
		
		#region Variables
		private int indicator = 0;
		private IDictionary<string, IOrder> entryOrders = new Dictionary<string, IOrder>();	
		private double priceR2 = 0.0;
		private bool inR2 = false;
		#endregion
		
			private VolSizeVar volSizeVar = new VolSizeVar();
			IDictionary<string, int[]> VTypeContainer = new Dictionary<string, int[]>();
			private DateTime EETime;
		
		private int ConvertZone(string Time){ // HHmmss
			DateTime TDay = DateTime.Today;
			TDay = TDay.AddHours(int.Parse(Time.Substring(0, 2))).AddMinutes(int.Parse(Time.Substring(2, 2))).AddSeconds(int.Parse(Time.Substring(4, 2)));	//time for the referenced market
			TDay = TDay.AddSeconds(-_GMT_MKT);	// Change the Time to UTC
			TDay = TDay.AddSeconds(_GMT);	// Change to Local Time
					
			int retVal = int.Parse(TDay.ToString("HHmmss"));
			if (retVal == 0)
				retVal = 240000;
			return retVal;
			
		}
		
		//protected override void Initialize(){
		protected override void Init(){
			HLCache = new HLDO(lookBackPeriod);
			//SyncAccountPosition = true;	//testing 28/2/2012; in conj with WUFBEL
			BarsRequired = 40;	//instead of 20 or 1448
			Enabled = true;
			ExitOnClose = false;
			EntriesPerDirection = 2;
			CalculateOnBarClose = true;

			if (Instrument.FullName.StartsWith("SI")){
				this._Multiplier = 200; this._StartTime = ConvertZone("072500"); this._CutOffTime = ConvertZone("120500"); this._EndTime = ConvertZone("122500"); 
				this._eStartTime = ConvertZone("190000"); this._eFlatTime = ConvertZone("161000"); this._eEndTime = ConvertZone("161500");
			}
			else if (Instrument.FullName.StartsWith("CL")){	
				this._Multiplier = 100;	this._StartTime = ConvertZone("080000"); this._CutOffTime = ConvertZone("131000"); this._EndTime = ConvertZone("133000"); 
				this._eStartTime = ConvertZone("190000"); this._eFlatTime = ConvertZone("161000"); this._eEndTime = ConvertZone("161500");
			}
			else if (Instrument.FullName.StartsWith("GC")){
				this._Multiplier = 10; this._StartTime = ConvertZone("072000"); this._CutOffTime = ConvertZone("121000"); this._EndTime = ConvertZone("123000"); 
				this._eStartTime = ConvertZone("190000"); this._eFlatTime = ConvertZone("161000"); this._eEndTime = ConvertZone("161500");
				EETime = new DateTime(2000,1,1,1,30,0,0);	
			}
			else if (Instrument.FullName.StartsWith("HG")){
				this._Multiplier = 2000; this._StartTime = ConvertZone("071000"); this._CutOffTime = ConvertZone("114000"); this._EndTime = ConvertZone("120000"); 
				this._eStartTime = ConvertZone("190000"); this._eFlatTime = ConvertZone("161000"); this._eEndTime = ConvertZone("161500");
				EETime = new DateTime(2000,1,1,1,30,0,0);	
			}
			else if (Instrument.FullName.StartsWith("FDAX")){
				_GMT_MKT = 2 * 3600; 
				this._Multiplier = 2; //this.timing1 = this._CutOffTime; this.timing2 = 999999;
				this._StartTime = ConvertZone("090000"); this._CutOffTime = ConvertZone("174000"); this._EndTime = ConvertZone("180000"); 
				this._eStartTime = ConvertZone("083000"); this._eFlatTime = ConvertZone("215500"); this._eEndTime = ConvertZone("220000");			
			}
			else if (Instrument.FullName.StartsWith("FGBL")){
				_GMT_MKT = 2 * 3600; 
				this._Multiplier = 100; //this.timing1 = this._CutOffTime; this.timing2 = 999999;
				this._StartTime = ConvertZone("090000"); this._CutOffTime = ConvertZone("174000"); this._EndTime = ConvertZone("180000"); 
				this._eStartTime = ConvertZone("083000"); this._eFlatTime = ConvertZone("215500"); this._eEndTime = ConvertZone("220000");			
			}
			else if (Instrument.FullName.StartsWith("6E")){
				this._Multiplier = 10000; this._StartTime = ConvertZone("072000"); this._CutOffTime = ConvertZone("134000"); this._EndTime = ConvertZone("140000"); 
				this._eStartTime = ConvertZone("190000"); this._eFlatTime = ConvertZone("155500"); this._eEndTime = ConvertZone("160000");	
			}
			
			stoplosstab.Add("GC", 520.0);stoplosstab.Add("SI", 1300.0); stoplosstab.Add("6E", 350.0); stoplosstab.Add("CL", 520.0); stoplosstab.Add("FDAX", 1275.0);stoplosstab.Add("HG", 525.0); // give 2 tick leeway
			//stoplosstab.Add("GC", 220.0);stoplosstab.Add("SI", 1300.0); stoplosstab.Add("6E", 350.0); stoplosstab.Add("CL", 520.0); stoplosstab.Add("FDAX", 1275.0); // give 2 tick leeway
			marginDebited.Add("GC", 1000.0); marginDebited.Add("SI", 2500.0); marginDebited.Add("6E", 500.0); marginDebited.Add("CL", 1000.0); marginDebited.Add("FDAX", 2500.0); //FDAX in euro not accounted for yet
			relativeWeight.Add("GC", 1000.0); relativeWeight.Add("SI", 2000.0); relativeWeight.Add("6E", 500.0); relativeWeight.Add("CL", 1000.0); relativeWeight.Add("FDAX", 2000.0); 
			
			for (int i = 0; i < 14; i++){
				bolll[i] = 0;
				bolls[i] = 0;
			}
			for (int i = 0; i < 6; i++){
				shortBollAvg[0] = 0;
				longBollAvg[0] = 0;
			}			
			
			volSizeVar.R1Slabels.Clear(); volSizeVar.R1Llabels.Clear();	volSizeVar.R2Slabels.Clear(); volSizeVar.R2Llabels.Clear();
			
			for (int version = 1; version <= 5; version++){
				volSizeVar.R1Slabels.Add(version + "_" + 4,Instrument.FullName + " " + DirectionType.S + PhaseType.E + "V" + version + "F"); 
				volSizeVar.R1Slabels.Add(version + "_" + 2,Instrument.FullName + " " + DirectionType.S + PhaseType.E + "V" + version + "H"); 
				volSizeVar.R1Slabels.Add(version + "_" + 1,Instrument.FullName + " " + DirectionType.S + PhaseType.E + "V" + version + "Q");
				volSizeVar.R1Llabels.Add(version + "_" + 4,Instrument.FullName + " " + DirectionType.L + PhaseType.E + "V" + version + "F"); 
				volSizeVar.R1Llabels.Add(version + "_" + 2,Instrument.FullName + " " + DirectionType.L + PhaseType.E + "V" + version + "H"); 
				volSizeVar.R1Llabels.Add(version + "_" + 1,Instrument.FullName + " " + DirectionType.L + PhaseType.E + "V" + version + "Q");		
			}
			
			volSizeVar.R2Slabels.Add("0_" + 4, Instrument.FullName + " " + DirectionType.S + PhaseType.E + "FR2"); 
			volSizeVar.R2Slabels.Add("0_" + 2, Instrument.FullName + " " + DirectionType.S + PhaseType.E + "HR2"); 
			volSizeVar.R2Slabels.Add("0_" + 1, Instrument.FullName + " " + DirectionType.S + PhaseType.E + "QR2"); 
			volSizeVar.R2Llabels.Add("0_" + 4, Instrument.FullName + " " + DirectionType.L + PhaseType.E + "FR2"); 
			volSizeVar.R2Llabels.Add("0_" + 2, Instrument.FullName + " " + DirectionType.L + PhaseType.E + "HR2"); 
			volSizeVar.R2Llabels.Add("0_" + 1, Instrument.FullName + " " + DirectionType.L + PhaseType.E + "QR2");		
			
			this.Multiplier = this._Multiplier;
			int[] V1 = new int[]{0, 1, 2}; 
			int[] V2 = new int[]{0, 1, 3}; 
			int[] V3 = new int[]{0, 2, 3}; 
			int[] V4 = new int[]{0, 1, 2, 3}; 
			int[] V5 = new int[]{0, 1, 2, 3, 4};
			
			VTypeContainer.Add("V0", V1); 
			VTypeContainer.Add("V1", V1); 
			VTypeContainer.Add("V2", V2); 
			VTypeContainer.Add("V3", V3); 
			VTypeContainer.Add("V4", V4); 
			VTypeContainer.Add("V5", V5);
		}
		
		public double DoRoundMult(double val){
			return Math.Round(Multiplier * val, 0, MidpointRounding.AwayFromZero);
		}
		
		public double DoRoundNoMult(double val){
			return Math.Round(val, 0, MidpointRounding.AwayFromZero);
		}
		
		private static StreamReader myStreamReader = null;
		private static string path = "C:\\\\Documents and Settings\\User\\Desktop\\myAccountSize.dat";
		private static bool hasAccountSize = false;
		//private static double myAccountSize = 0;
		private double myAccountSize = 0;
		private static List<double> SaveQueue = new List<double>();
		private static bool isUpdatingAccountSize = false;

		private void retrieveAccountValue(){	//This Stores the value of the account into the current value into a text file.
	//		if (hasRetrievedAccountSize){
	//			Print("The Account Size has already been retrieved for this session.");
	//			return;
	//		}
			
	//		if (!File.Exists(path)) // Check if file exists
	//			File.Create(path);
	//		
	//		if (myStreamReader == null)
	//			myStreamReader = new StreamReader(path);
	//		
	//		try{
	//			String ActSize = myStreamReader.ReadLine();
	//			myAccountSize = long.Parse(ActSize);
	//			hasAccountSize = true;
	//			//hasRetrievedAccountSize = true;
	//			if (GetAccountValue(AccountItem.CashValue) == 0 && myAccountSize < 10000d)
	//				myAccountSize = 100000d;
	//			Print("Account size retrieved: " + myAccountSize.ToString());
	//		}
	//		catch (Exception Ex){
	//			Print("Exception:: " + Ex.Message);
	//			Print("Your file was either missing or there was nothing to read from. Cease Trading...");
	//		}
	//		myStreamReader.Close(); // Closing the IO file
	//		myStreamReader.Dispose();
	//		myStreamReader = null;
			
			this.AccountSize = 10000;
		}
		
		private void SaveAccountSize(double amount){
	//		if (hasAccountSize && !isUpdatingAccountSize){
	//			isUpdatingAccountSize = true;
	//			myAccountSize += amount;
	//			StreamWriter myStreamWriter = new StreamWriter(path, false);
	//			myStreamWriter.WriteLine(myAccountSize);
	//			Print("saving Amount of: " + amount + " Final Account size of: " + myAccountSize);
	//			myStreamWriter.Flush();
	//			myStreamWriter.Close();
	//			myStreamWriter.Dispose();
	//			myStreamWriter = null;
	//			isUpdatingAccountSize = false;
	//			
	//			if (SaveQueue.Count != 0){
	//				amount = SaveQueue[0];
	//				SaveQueue.RemoveAt(0);
	//				SaveAccountSize(amount);
	//			}
	//		}
	//		else if (isUpdatingAccountSize){
	//			SaveQueue.Add(amount);
	//		}
		}
		
		protected void HandleOrderUpdate(IOrder order){			
		
			if (order.OrderState == OrderState.Accepted){
				if (order.OrderAction == OrderAction.Buy){	// Handles Longs (Buying:Entry)
					Print("Handles Longs (Buying:Entry): Order Quantity: " + order.Quantity + "AvgFillPrice: " + order.AvgFillPrice);
					SaveAccountSize(-order.Quantity * marginDebited[order.Instrument.MasterInstrument.Name]);
				}
				else if (order.OrderAction == OrderAction.Sell){	// Handle Longs (Selling:Exit)
					if (Performance.AllTrades.TradesCount == 0)
						Print("Trade Count was 0 so we'll wait and see...");
					else{
						Print("Performance.AllTrades.TradesCount: " + Performance.AllTrades.TradesCount);
						Print("Handle Longs (Selling:Exit): Order Quantity: " + order.Quantity + "AvgFillPrice: " + order.AvgFillPrice + "PnL: " + 
							Performance.AllTrades[Performance.AllTrades.TradesCount - 1].ProfitCurrency * Performance.AllTrades[Performance.AllTrades.TradesCount - 1].Quantity);
						SaveAccountSize(order.Quantity * marginDebited[order.Instrument.MasterInstrument.Name] );
						SaveAccountSize(Performance.AllTrades[Performance.AllTrades.TradesCount - 1].ProfitCurrency * Performance.AllTrades[Performance.AllTrades.TradesCount - 1].Quantity);
					}
				}
				else if (order.OrderAction == OrderAction.SellShort){	// Handles Shorts (Sell Short:Entry)
					Print("Handles Shorts (Sell Short:Entry): Order Quantity: " + order.Quantity + "AvgFillPrice: " + order.AvgFillPrice);
					SaveAccountSize(-order.Quantity * marginDebited[order.Instrument.MasterInstrument.Name]);
				}
				else if (order.OrderAction == OrderAction.BuyToCover){	// Handle Shorts (Buy To Cover:Exit)
					if (Performance.AllTrades.TradesCount == 0)
						Print("Trade Count was 0 so we'll wait and see...");
					else{
						Print("Performance.AllTrades.TradesCount: " + Performance.AllTrades.TradesCount);
						Print("Handle Shorts (Buy To Cover:Exit): Order Quantity: " + order.Quantity + "AvgFillPrice: " + order.AvgFillPrice + "PnL: " + 
							Performance.AllTrades[Performance.AllTrades.TradesCount - 1].ProfitCurrency * Performance.AllTrades[Performance.AllTrades.TradesCount - 1].Quantity);
						SaveAccountSize(order.Quantity * marginDebited[order.Instrument.MasterInstrument.Name]);
						SaveAccountSize(Performance.AllTrades[Performance.AllTrades.TradesCount - 1].ProfitCurrency * Performance.AllTrades[Performance.AllTrades.TradesCount - 1].Quantity); 
					}
				}	
			}			
		}

		protected override void OnTermination(){
			//SaveAccountSize(0);
			SaveAccountSize(-myAccountSize);
			Print("Total Trades: " + Performance.AllTrades.Count);
			
			//TerminateCustomThreads();
			//BackTestManager.getInstance().TerminateCustomThreads();
			
			// Print Trades
			//PrintTrades();
		}
		/*
		private void PrintTrades()
		{
			Print("Test SIM Strategy Terminated");
			
			// Print out Results of BTest
			Print("Print out Results of BTest");
			StringWriter fullTradeString = new StringWriter();
			String tradeString = "";
			// Print Short Trades
			foreach ( ITrade itrade in this.shortTradersCompleted )
			{
				tradeString = "";
				
				tradeString += (itrade.getIsLong() ? "LONG" : "SHORT") + "\t" + ( itrade.getIsSim() ? "SIM":"REAL" );
			
				try
				{
					tradeString += "\t" + itrade.getEntryOrder().Name + "\t" + itrade.getEntryOrder().AvgFillPrice +  "\t" + itrade.getEntryOrder().Time +  "\t" + itrade.getEntryOrder().Quantity;
					tradeString += "\t" + itrade.getExitOrder().Name + "\t" + itrade.getExitOrder().AvgFillPrice +  "\t" + itrade.getExitOrder().Time +  "\t" + itrade.getExitOrder().Quantity;
				}
				catch ( Exception Ex )
				{
					Print(Ex.Message);
				}
				
				Print(tradeString);
				//fullTradeString.WriteLine( tradeString );
			}
			
			// Print Long Trades
			foreach ( ITrade itrade in this.longTradersCompleted )
			{
				tradeString = "";
				
				tradeString += (itrade.getIsLong() ? "LONG" : "SHORT") + "\t" + ( itrade.getIsSim() ? "SIM":"REAL" );
			
				try
				{
					tradeString += "\t" + itrade.getEntryOrder().Name + "\t" + itrade.getEntryOrder().AvgFillPrice +  "\t" + itrade.getEntryOrder().Time +  "\t" + itrade.getEntryOrder().Quantity;
					tradeString += "\t" + itrade.getExitOrder().Name + "\t" + itrade.getExitOrder().AvgFillPrice +  "\t" + itrade.getExitOrder().Time +  "\t" + itrade.getExitOrder().Quantity;
				}
				catch ( Exception Ex )
				{
					Print(Ex.Message);
				}
				
				Print(tradeString);
				//fullTradeString.WriteLine( tradeString );
			}
			
			//Print( fullTradeString.ToString() );
			//fullTradeString.Close();
			// Print Long Trades
		}
		*/
		
		protected override void OnStartUp(){
		//protected override void StartUp(){
			//ComputeMovingWindow();	// Compute Moving Window for each Market
			//retrieveAccountValue();	// Retrieving last account value from our Text file.
			configureVTimings();	
		}
		
		public void configureVTimings(){
			DateTime currentTime = Time[0];	// Configure the Start, End and CufOff Timings for all the Vs
			DateTime ST = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 
				int.Parse(this._StartTime.ToString("D6").Substring(0,2)), int.Parse(this._StartTime.ToString("D6").Substring(2,2)), int.Parse(this._StartTime.ToString("D6").Substring(4,2)), 00);
			bool is12am = false;
			int myHr = int.Parse(this._EndTime.ToString("D6").Substring(0,2));
			if (myHr > 23){
				is12am = true;
				myHr  = 0;
			}

			DateTime ET = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, myHr, int.Parse(this._EndTime.ToString("D6").Substring(2,2)), int.Parse(this._EndTime.ToString("D6").Substring(4,2)), 00);
			if (is12am)
				ET.AddDays(1);
			if (ST > ET)
				ET = ET.AddDays(1);
			DateTime CT = ET.AddMinutes(-20);
			DateTime eST = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, int.Parse(this._eStartTime.ToString("D6").Substring(0,2)), 
						int.Parse(this._eStartTime.ToString("D6").Substring(2,2)), int.Parse(this._eStartTime.ToString("D6").Substring(4,2)), 00);
			DateTime eET = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, int.Parse(this._eEndTime.ToString("D6").Substring(0,2)), 
						int.Parse(this._eEndTime.ToString("D6").Substring(2,2)), int.Parse(this._eEndTime.ToString("D6").Substring(4,2)), 00);
			if (eST > eET)
				eET = eET.AddDays(1);
		
			this.VTimingDic.Add("V1", new VTimings(VType.V1, ST, ET, CT));
			this.VTimingDic.Add("V2", new VTimings(VType.V2, ST, ET, CT));
			this.VTimingDic.Add("V3", new VTimings(VType.V3, ST, ET, CT));
			this.VTimingDic.Add("V4A", new VTimings(VType.V4, eST, ST, ST));	
			this.VTimingDic.Add("V4B", new VTimings(VType.V4, ET.AddMinutes(-19), eET, eET.AddMinutes(-20))); // start immediately when cash cutoff 
			this.VTimingDic.Add("V5", new VTimings(VType.V5, eST, eET, eET.AddMinutes(-20)));
			this.VTimingDic.Add("V0", new VTimings(VType.V0, eST, eET, eET.AddMinutes(-20)));	// V0 is explicitly for R2
		}
		
		private static double maxCumProfit = 0;
		private static int unitTrade = 10;
		private static double maxPermittedLoss = 10000;
		
		public void ComputeMovingWindow(){
			double totalProfits = 0;
			foreach (Trade trade in Performance.AllTrades){	// Retrieve and find out the largest QTy for trade
				totalProfits += trade.ProfitCurrency ;
				maxCumProfit = Math.Max(maxCumProfit, totalProfits);
			}
			Print("Max Profit: " + maxCumProfit + " out of " + Performance.AllTrades.Count + " trades.");
		}

		protected void updateBollinger(){	
			bolll[CurrentBar % 14] = DoRoundNoMult(_Multiplier * (Bollinger(2,14).Upper[0])) - DoRoundNoMult(_Multiplier * (Bollinger(2,14).Middle[0]));
			bolls[CurrentBar % 14] = DoRoundNoMult(_Multiplier * (Bollinger(2,14).Middle[0])) - DoRoundNoMult(_Multiplier * (Bollinger(2,14).Lower[0]));
			double s = bolls[0];
			double l = bolll[0];
			
			for (int i = 1; i < 14; i++){
				l += bolll[i];
				s += bolls[i];
			}
			for (int i = (this.longBollAvg.Length - 1); i > 0; i--){
				this.longBollAvg[i] = this.longBollAvg[i - 1];
				this.shortBollAvg[i] = this.shortBollAvg[i - 1];
			}
			this.longBollAvg[0] = l / 14;
			this.shortBollAvg[0] = s / 14;	
		}

		double previousprice = 0.0;
		private ArrayList stopLossTokens = new ArrayList();
		
		
		//public override void orderUpdate(IOrder order){	
		protected override void OnOrderUpdate(IOrder order)
		{
				
			//Print("ORDER UPDATE>> TIME: " + Time[0] + " NAME: " + order.Name + "  STATE: " + order.OrderState + "  TYPE: " + order.OrderType + " ACTION: " + order.OrderAction + " FILL PRICE: "+order.AvgFillPrice);
			//Utility.log("ORDER UPDATE>> TIME: " + Time[0] + " NAME: " + order.Name + "  STATE: " + order.OrderState + "  TYPE: " + order.OrderType + " ACTION: " + order.OrderAction + " FILL PRICE: "+order.AvgFillPrice);
			Utility.log("ORDER UPDATE>> TIME: " + Time[0].ToString("DD HH:mm:ss.fff ") + " NAME: " + order.Name + "  STATE: " + order.OrderState + "  TYPE: " + order.OrderType + " ACTION: " + order.OrderAction + " FILL PRICE: "+order.AvgFillPrice);
			
			if (order.OrderType != OrderType.Stop){
				if (order.OrderAction == OrderAction.Buy || order.OrderAction == OrderAction.SellShort){ // Passes in the Order for this marker to make it the entry order if it is an existing trade
					if (!entryOrders.ContainsKey(order.Name)){	// Adds a freshly opened order into the EntryOrder List
	//                  Print("Added Order for " + order.Name);
						entryOrders.Add(order.Name, order);
					}
				}
				else{
					foreach (Trade trade in Performance.AllTrades){	// Removes an order thats already closed or is closing
						if (entryOrders.ContainsKey(trade.Entry.Name)){
	//                      Print("Remove:" + trade.Entry.Name);
							entryOrders.Remove(trade.Entry.Name);
						}
					}
					//else
	//	                Print("Order Name: " + order.Name + " could not be found in list...");
				}
			}
			//HandleOrderUpdate(order);		// This handles the Computations of the Account Size after an order(specifically this one) was update

			if (order.OrderState == OrderState.PendingSubmit){		// If OnOrderUpdate() is called from a stop loss or profit target order add its token to the appropriate collection	
				if (order.Name == "Stop loss"){		// Add the "Stop loss" orders to the Stop Loss collection
					stopLossTokens.Add(order.Token);
				}
				else if (order.Name.Contains("SX") || order.Name.Contains("LX")){	//regular exit; check that no stop loss was triggered yet
				}
			}		
			if (stopLossTokens.Contains(order.Token)){	// Process stop loss orders
				if (/*order.OrderState == OrderState.Cancelled || */ order.OrderState == OrderState.Filled /*|| order.OrderState == OrderState.Rejected*/){		// Check order for terminal state
	//              Print(order.ToString());	// Print out information about the order
					stopLossTokens.Remove(order.Token);	// Remove from collection
				}	
	//		    else	// Print out the current stop loss price
	//              Print("The order name " + order.Name + " stop price is currently " + order.StopPrice);
			}
		}
		/*
		//private void CheckLicence(){
		{
			maxCumProfit = Math.Max(maxCumProfit, Performance.AllTrades.TradesPerformance.Currency.CumProfit);
			//Print("Max Profit: " + maxCurrentProfit + " out of " + Performance.AllTrades.Count + " trades. Current Cum Profit: " + Performance.AllTrades.TradesPerformance.Currency.CumProfit);
			double movingWindowOfPastXTrades = 0;
			double movingWindowOfPastXTradesResultant = 0;
			//int Limit = Math.Min(unitTrade, Performance.AllTrades.Count);
			if (unitTrade <= Performance.AllTrades.Count){
				for (int i = 0 ; i < unitTrade; i++)
					movingWindowOfPastXTrades += Performance.AllTrades[Performance.AllTrades.Count - (i + 1)].Quantity;
					movingWindowOfPastXTradesResultant = movingWindowOfPastXTrades / unitTrade;
	//				Print(Performance.AllTrades.Count + ". " + movingWindowOfPastXTradesResultant + "  movingWindowOfPastXTrades: " + movingWindowOfPastXTrades + " unitTrade: " + unitTrade);
					string resultantString = "";
				
				if (stoplosstab.ContainsKey(Instrument.MasterInstrument.Name))
					maxPermittedLoss = (stoplosstab[Instrument.MasterInstrument.Name] * movingWindowOfPastXTradesResultant);			
			}
			else
				maxPermittedLoss = 1000000.0;
	//          Print(Performance.AllTrades.Count + ". maxCumProfit: " + maxCumProfit + " vs " + (maxPermittedLoss + Performance.AllTrades.TradesPerformance.Currency.CumProfit));
		}
		*/
		/*
		public override bool checkLicense()
		{
			return false;
		}
		*/
		
		protected override void OnExecution(IExecution execution){	
			//	CheckLicence();
			if (execution!=null){
				if (execution.Order.OrderAction == OrderAction.BuyToCover || execution.Order.OrderAction == OrderAction.Sell){
					indicator = 0;
					if (inR2){
					//if (entryOrders.Count > 1){
						inR2 = false; 
						r2entrybar = -1;
					}
				}
				else if (execution.Name.Contains("R2")){
					R2signal = execution.Name;
					priceR2 = execution.Price;
					inR2 = true;
					r2entrybar = CurrentBar;
				}
				else{
					R2signal = "";
					ourEntryPrice = execution.Price;
				}
			}
		}
		
		int highesthighindex = 0; int lowestlowindex = 0;
		int flattenclgcsi; int flatten2clgcsi; int flattenfd; int flatten2fd; int flatten6e; int flatten26e;
		bool trendCross = false; bool trendCrossShort = false; bool INtrendCrossShort = true; bool INtrendCrossLong = true;
			
		//public override void update(){	
		protected override void OnBarUpdate(){	
			INtrendCrossShort = false;
			INtrendCrossLong = false;
			
			if (HLCache != null){ // Update HH and LL for valid open positions
				if (HLCache.getHighestHigh().getIsActive()){
					HLCache.getHighestHigh().checkNewHL(this);
					this.highestHigh = DoRoundMult(HLCache.getHighestHigh().getValue());
				}
				
				if (HLCache.getLowestLow().getIsActive()){
					HLCache.getLowestLow().checkNewHL(this);
					this.lowestLow = DoRoundMult(HLCache.getLowestLow().getValue());
				}
			}
				
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				int lookback = 20;	//still testing
				for (int i = 0; i < lookback; i++){
					if (DoRoundNoMult(DM(14).DiMinus[i]) - DoRoundNoMult(ADX(14)[i]) > 0 /*part A*/	
						&& DoRoundNoMult(DM(14).DiPlus[i]) - DoRoundNoMult(DM(14).DiMinus[i]) >= 0
						&& 2 * DoRoundNoMult(ADX(14)[0]) < Math.Truncate(1 + (DoRoundNoMult((DM(14).DiMinus[0]) + (DM(14).DiPlus[0]))))	//dont ever change this format
						){
						INtrendCrossLong = true;
						i = lookback + 1;
					}
					if (DoRoundNoMult(ADX(14)[i]) - DoRoundNoMult(DM(14).DiPlus[i]) > 0 /*partB; minus - adx >0*/ 
						&& DoRoundNoMult(DM(14).DiPlus[i]) - DoRoundNoMult(DM(14).DiMinus[i]) > 0
						&& 2 * DoRoundNoMult(ADX(14)[0]) < DoRoundNoMult(1 + ((DM(14).DiMinus[0] + DM(14).DiPlus[0])))
						){
						INtrendCrossLong = true;
						i = lookback + 1;
					}					
				}
				for (int i = 0; i < lookback; i++){
					if (DoRoundNoMult(DM(14).DiPlus[i]) - DoRoundNoMult(ADX(14)[i]) > 0 
						&& DoRoundNoMult(DM(14).DiMinus[i]) - DoRoundNoMult(DM(14).DiPlus[i]) >= 0
						&& 2 * DoRoundNoMult(ADX(14)[0]) < Math.Truncate(1 + (DoRoundNoMult((DM(14).DiMinus[0]) + (DM(14).DiPlus[0]))))	//dont ever change this format
						){
						INtrendCrossShort = true;
						i = lookback + 1;
					}
					if (DoRoundNoMult(ADX(14)[i]) - DoRoundNoMult(DM(14).DiMinus[i]) > 0 
						&& DoRoundNoMult(DM(14).DiMinus[i]) - DoRoundNoMult(DM(14).DiPlus[i]) > 0
						&& 2 * DoRoundNoMult(ADX(14)[0]) < DoRoundNoMult(1 + ((DM(14).DiMinus[0] + DM(14).DiPlus[0])))
						){
						INtrendCrossShort = true;
						i = lookback + 1;
					}
				}
			}
			
			DSTAddMinus(10000, 1);
			
			this.updateBollinger();
			testMALongExits(true, false);
			testMALongExits(false, false);
	//
	//		#region MA Long	
			MATester(VTypeContainer["V0"], 1, true, true); 
			MATester(VTypeContainer["V1"], 1, true, false);	
			MATester(VTypeContainer["V2"], 2, true, false);	
			MATester(VTypeContainer["V3"], 3, true, false);
			MATester(VTypeContainer["V4"], 4, true, false);	
			MATester(VTypeContainer["V5"], 5, true, false);	
	//		#endregion
	//			
	//		#region MA Short			
			MATester(VTypeContainer["V0"], 1, false, true);	
			MATester(VTypeContainer["V1"], 1, false, false); 
			MATester(VTypeContainer["V2"], 2, false, false); 
			MATester(VTypeContainer["V3"], 3, false, false);
			MATester(VTypeContainer["V4"], 4, false, false); 
			MATester(VTypeContainer["V5"], 5, false, false);
	//		#endregion

			DSTAddMinus(-10000, 2);	
		}	
			
		private void DSTAddMinus(int TimeShift, int phaseIndex){
			if (!this.daylightsavings() && phaseIndex == 1){	
				flattenclgcsi = ConvertZone("171000"); flatten2clgcsi = ConvertZone("171400"); flattenfd = ConvertZone("225500"); flatten2fd = ConvertZone("225900"); flatten6e = ConvertZone("165500"); flatten26e = ConvertZone("165900");
				this._StartTime = this._StartTime + TimeShift; this._CutOffTime = this._CutOffTime + TimeShift;	this._EndTime = this._EndTime + TimeShift;
			}
			if ( phaseIndex == 2){
				flattenclgcsi = ConvertZone("161000"); flatten2clgcsi = ConvertZone("161400"); flattenfd = ConvertZone("215500"); flatten2fd = ConvertZone("215900"); flatten6e = ConvertZone("155500"); flatten26e = ConvertZone("155900");
				this._StartTime = this._StartTime - TimeShift; this._CutOffTime = this._CutOffTime - TimeShift; this._EndTime = this._EndTime - TimeShift;
			}
		}
		
		System.Collections.Generic.List<String> exitNames = new System.Collections.Generic.List<String>();
		System.Collections.Generic.List<String> exitNamesShort = new System.Collections.Generic.List<String>();
		protected void getOut(String exitSymbol, bool isLong){
			Print("Getting Out: " + exitSymbol);
			
			if (isLong){
				if (!exitSymbol.EndsWith("_R2"))
					foreach (String value in volSizeVar.R1Llabels.Values){
						ExitLong(Instrument.FullName + " " + exitSymbol, value);
						this.exitbegun = true;
					}
					
				foreach (String value in volSizeVar.R2Llabels.Values){
					ExitLong(Instrument.FullName + " " + exitSymbol, value);
					//HLCache.getHighestHigh().kill(); // = null; // Add tracker the HH and LL
				}
				indicator = 0;
			}
			else{
				if (!exitSymbol.EndsWith("_R2"))
					foreach (String value in volSizeVar.R1Slabels.Values){
						ExitShort(Instrument.FullName + " " + exitSymbol, value);
						this.exitbegun = true;		
					}	
				foreach (String value in volSizeVar.R2Slabels.Values){
					ExitShort(Instrument.FullName + " " + exitSymbol, value);
					//HLCache.getLowestLow().kill(); // Add tracker the HH and LL
				}
				indicator = 0;
			}
			ourEntryPrice = 0.0;	
		}

		protected void MATester(int[] cells,int version, bool isLong, bool isR2){
			if ( isR2 ){
				if ( Position.MarketPosition == MarketPosition.Flat )
					return;
				else if (( (isLong && Position.MarketPosition == MarketPosition.Short) || (!isLong && Position.MarketPosition == MarketPosition.Long))  /*&& (version >= 4)*/  )
					isR2 = !isR2;
			}

			if (checkEntryTimings(cells, version, isR2)
				&& Section0(cells, new int[]{}, version, isLong, isR2)		//no r2 TEST!
				&& Section00(cells, new int[]{}, version, isLong, isR2)
				&& Section1(cells, new int[]{}, version, isLong, isR2) 
				&& Section2(cells, new int[]{}, version, isLong, isR2)		//no r2; //definitively include v5 	
				&& Section3(cells, new int[]{4,5}, version, isLong, isR2)	//no r2 TEST! also exclude 4 and 5
				//&& Section4(cells, new int[]{}, version, isLong, isR2)	
				&& Section5(cells, new int[]{}, version, isLong, isR2)	
				//&& Section6(cells, new int[]{}, version, isLong, isR2)	
				&& Section7(cells, new int[]{5}, version, isLong, isR2)		//no r2 TEST!
				&& Section8(cells, new int[]{}, version, isLong, isR2)	
				&& Section9(cells, new int[]{}, version, isLong, isR2)	
				&& Section10(cells, new int[]{}, version, isLong, isR2)
				&& Section11(cells, new int[]{}, version, isLong, isR2)	
			){
				
//				if ( ((isLong && Position.MarketPosition == MarketPosition.Short) || (!isLong && Position.MarketPosition == MarketPosition.Long)) && (version >= 4) ){	//where v45 works..// Check for inverse entry
//					//Print("Trying for a Flip... Version: " + version);
//					Entrys(cells, version, isLong, false);
//				}else //actually elseif
					if ( Position.MarketPosition == MarketPosition.Flat ){	// Check for Flat Position
					//Print("Trying for a entry on flat... Version: " + version);
					Entrys(cells, version, isLong, false);
				}
				else if (isR2){		// Check for R2
					bool canR2 = false;
					if (BarsSinceEntry() > 3 && BarsSinceEntry() < 16 
						&& (DoRoundMult(Open[2]) - DoRoundMult(Open[BarsSinceEntry()])) * getDirection(isLong) > 1
						&& getDirection(isLong) * 2 * (DoRoundMult(Close[0]) - DoRoundMult(Open[0])) > DoRoundMult(Range()[0]) //testing
					){ 	
						if ((isLong && Position.MarketPosition == MarketPosition.Long) || (!isLong && Position.MarketPosition == MarketPosition.Short)){
							//Print("Trying for an R2 entry... Version: " + version);
							Entrys(cells, version, isLong, isR2);
						}
					}
				}
			}   
		}	

		public static Dictionary<string, string> ActiveLabels = null;
		//protected void Entrys(int[] cells, int version, bool isLong, bool isR2){	//Tied to MATesterShort
		protected void Entrys(int[] cells, int version, bool isLong, bool isR2){	//Tied to MATesterShort
			int size = 3;	//for f,h and q
			int testLimit = cells.Length;	//v45 use cells.length 
			string VersionLabel = version.ToString();
			PhaseType phaseType = PhaseType.X;
			DirectionType directionType = isLong ? DirectionType.L : DirectionType.S; 
			
			if (isR2)
				VersionLabel = "0";
			if (isR2 && isLong)
				ActiveLabels = volSizeVar.R2Llabels;
			else if (isR2 && !isLong)
				ActiveLabels = volSizeVar.R2Slabels;	
			else if (!isR2 && isLong)
				ActiveLabels = volSizeVar.R1Llabels;	
			else if (!isR2 && !isLong)
				ActiveLabels = volSizeVar.R1Slabels;	
			
			int BetQuantity;
			int fullBetQty;

			for (int i = 0 ; i < size; i++){
				if (VPositionTester(cells, testLimit, size, i, isR2)){
					if (!isLong){	// For R2 and R1 Shorts  		!isLong !(isLong = false)
						if (Position.MarketPosition == MarketPosition.Long /*&& !isR2*/){
							Print(getExitLabel(directionType, phaseType, ExitLetterType.Q, isR2));
							getOut(getExitLabel(directionType, phaseType, ExitLetterType.Q, isR2), isLong);
						}
							fullBetQty = (int)Math.Pow(VolSizeVar.betSizing, (size - (i + 1)));
	//						SetStopLoss(ActiveLabels[VersionLabel + "_" + fullBetQty], CalculationMode.Price, Close[0] + getDirection(!isLong) * stoplosstab[Instrument.MasterInstrument.Name] / Instrument.MasterInstrument.PointValue, false);
	//						EnterShort(fullBetQty, ActiveLabels[VersionLabel + "_" + fullBetQty]);	
	//						if (!isR2)
	//							indicator = 1;
	//						break;}
						
					//	if ((Position.MarketPosition == MarketPosition.Flat && !isR2) || isR2)
						{
							//int fullBetQty = (int)Math.Pow(VolSizeVar.betSizing, (size - (i + 1)));
								
							BetQuantity = getBetQuantity(4,Volume[0]);
							//int BetQuantity = getBetQuantity(fullBetQty,Volume[0]);
							
							//if (BetQuantity > 0)
							{
								fullBetQty = (int)Math.Pow(VolSizeVar.betSizing, (size - (i + 1)));
								//fullBetQty = BetQuantity;
								SetStopLoss(ActiveLabels[VersionLabel + "_" + fullBetQty], CalculationMode.Price, Close[0] + getDirection(!isLong) * stoplosstab[Instrument.MasterInstrument.Name] / Instrument.MasterInstrument.PointValue, false);
								EnterShort(fullBetQty, ActiveLabels[VersionLabel + "_" + fullBetQty]);	
								
								//EnterShort(BetQuantity, Instrument.FullName+"SEV"+version+"F");	 // Remember to change Instrument.FullName to ActiveLabels
								HLCache.getLowestLow().start(); // Add tracker the HH and LL
								
								if (!isR2)
									indicator = 1;
								Print("Signal Name: " + ActiveLabels[VersionLabel + "_" + fullBetQty] + "	VersionLabel: " + VersionLabel + "	Date: " +  Time[0].ToString("  yyyy  MMM dd  HH:mm:ss" )  + " " +
								Volume[0] 
								//+ DoRoundNoMult((Open[cells[cells.Length - 1]] - Close[0])/(Convert.ToInt32(cells[cells.Length - 1])*ATR(14)[cells[cells.Length - 1]+1]))
//								+ " " + DoRoundNoMult((High[cells[cells.Length - 1]] - Low[cells[cells.Length - 1]])/(ATR(14)[cells[cells.Length - 1]+1]))  
//								+ " " + DoRoundNoMult((High[cells[cells.Length - 1]] - Low[cells[cells.Length - 1]])/(2*(ATR(14)[cells[cells.Length - 1]+1])))  
//								+ " " + DoRoundNoMult(ATR(14)[0]/ATR(14)[cells[cells.Length - 1]+1])
								);
								break;							
							}
						}
					}
					else
					{ 	// For R2 and R1 Longs
						if (Position.MarketPosition == MarketPosition.Short /*&& !isR2*/){
							Print(getExitLabel(directionType, phaseType, ExitLetterType.Q, isR2));
							getOut(getExitLabel(directionType, phaseType, ExitLetterType.Q, isR2), !isLong);  // false
						}
						
							fullBetQty = (int)Math.Pow(VolSizeVar.betSizing, (size - (i + 1)));
	//						SetStopLoss(ActiveLabels[VersionLabel + "_" + fullBetQty], CalculationMode.Price, Close[0] + getDirection(!isLong) * stoplosstab[Instrument.MasterInstrument.Name] / Instrument.MasterInstrument.PointValue, false);
	//						EnterLong(fullBetQty, ActiveLabels[VersionLabel + "_" + fullBetQty]);						
	//						if (!isR2)
	//							indicator = 1;		
	//						break;}
						//if ((Position.MarketPosition == MarketPosition.Flat && !isR2) || isR2)
						{
							//int fullBetQty = (int)Math.Pow(VolSizeVar.betSizing, (size - (i + 1)));
								
			//				int BetQuantity = getBetQuantity(4,Volume[0]);
							//int BetQuantity = getBetQuantity(fullBetQty,Volume[0]);
							
							//if (BetQuantity > 0)
							{
								fullBetQty = (int)Math.Pow(VolSizeVar.betSizing, (size - (i + 1)));
								//fullBetQty = BetQuantity;
								SetStopLoss(ActiveLabels[VersionLabel + "_" + fullBetQty], CalculationMode.Price, Close[0] + getDirection(!isLong) * stoplosstab[Instrument.MasterInstrument.Name] / Instrument.MasterInstrument.PointValue, false);
								EnterLong(fullBetQty, ActiveLabels[VersionLabel + "_" + fullBetQty]);						
								//EnterShort(BetQuantity, Instrument.FullName+"SEV"+version+"F");	 // Remember to change Instrument.FullName to ActiveLabels
								HLCache.getHighestHigh().start(); // Add tracker the HH and LL
								
								if (!isR2)
									indicator = 1;		
								Print("Signal Name: " + ActiveLabels[VersionLabel + "_" + fullBetQty] +  "	VersionLabel: " + VersionLabel + "	Date: " +  Time[0].ToString(" yyyy  MMM dd  HH:mm:ss" ) + " " +   
								Volume[0] 
								//+ DoRoundNoMult((Close[0] - Open[cells[cells.Length - 1]])/(Convert.ToInt32(cells[cells.Length - 1])*ATR(14)[cells[cells.Length - 1]+1]))
//								+ " " + DoRoundNoMult((High[cells[cells.Length - 1]] - Low[cells[cells.Length - 1]])/(ATR(14)[cells[cells.Length - 1]+1])) 
//								+ " " + DoRoundNoMult((High[cells[cells.Length - 1]] - Low[cells[cells.Length - 1]])/(2*(ATR(14)[cells[cells.Length - 1]+1]))) 
//								+ " " + DoRoundNoMult(ATR(14)[0]/ATR(14)[cells[cells.Length - 1]+1])
								);
						
								break;						
							}
						}
					}
				}
			}
		}	
		
		protected bool VPositionTester(int[] cells, int testLimit, int size, int index, bool isR2){	//defines size
			bool canEnter = true;
			double sizer = 1 + (size - (index + 1)) * (VolSizeVar.multiple / 100);
			testLimit = Math.Min(cells.Length, testLimit);

			if (!isR2)
				for (int i = 0; i < testLimit; i++)
					canEnter &= Volume[cells[i]] >= VOLMA(VolSizeVar.defaultLookBackInterval)[cells[i]] * sizer;		
			else
				for (int i = 0; i < testLimit; i++)
					canEnter &= Volume[i] >= VOLMA(VolSizeVar.defaultLookBackInterval)[cells[i]] * sizer;	//uniform?!
			return canEnter;
		}

		//private int getBetQuantity(int volumeSizing, double currentVolume)
		private int getBetQuantity(int fullBetQty, double currentVolume){
			if ( Instrument.FullName.StartsWith("FDAX") || Instrument.FullName.StartsWith("SI") )
				return fullBetQty;
			else if ( Instrument.FullName.StartsWith("GC") || Instrument.FullName.StartsWith("CL") )
				return fullBetQty * 2;
			else if ( Instrument.FullName.StartsWith("6E") )
				return fullBetQty * 4;
			
			return 0;
		}
		
		private IDictionary<string, double> RelativeSizingWeights = new Dictionary<string, double>(); 
		private int getBetQuantity2(int volumeSizing, double currentVolume){
			//sPrint("Volume[0]: " + currentVolume);
			//return 0;
			//Print("Cash Value (Just looking if its LIVE): " + GetAccountValue(AccountItem.CashValue));
			if (GetAccountValue(AccountItem.CashValue) != 0){
				//currentAccountValue = GetAccountValue(AccountItem.CashValue);
				Print("Account Size has bee updated from a SIM value of '" + myAccountSize + "' to a LIVE value of'" + GetAccountValue(AccountItem.CashValue) + "'");
				myAccountSize = GetAccountValue(AccountItem.CashValue);
			}
			
			//int Quantity = 0; // 0, 1, 5, 20;
			//Quantity = SQuantity;
			
			string[] s = Instrument.FullName.Split(' ');
			string ContractType = "";
			//double accountBalance = 100000d;
			
			if (s.Length != 0)
				ContractType = s[0];
			Print("\n 1 a). Account Value: "  + myAccountSize);		// 1a) Get Account Value of 100k	
			Print("\n 1 b). NA ");	// 1b) Check for Concurrent Trades
			
			double absoluteRiskAmount = myAccountSize * marketRiskPercentage; // 2. MAC (Max Allowed Contracts) / by marginDebited aka riskSizing
			int maxContractsAllowed = (int)Math.Floor(absoluteRiskAmount / (relativeWeight[ContractType]));	// 3. / by Relative Sizing Weights
			
			Print("2 and 3. maxContractsAllowed: " + maxContractsAllowed + "absoluteRiskAmount: " + absoluteRiskAmount + 
			"RelativeSizingWeights[" + ContractType + "]: " + RelativeSizingWeights[ContractType] + "using relativeWeight[" + ContractType + "]: " + relativeWeight[ContractType]);

			maxContractsAllowed = (int)Math.Floor((double)maxContractsAllowed * (double)volumeSizing / (double)4.0);	// 4. Volume Sizing
				
			if (maxContractsAllowed >= volumeSizing)	// This is mainly to size down for large trades. However, for currently smaller quantities of lesser than 4, we'll just take it for now. 
				maxContractsAllowed = maxContractsAllowed - (maxContractsAllowed % volumeSizing);
			Print("4. maxContractsAllowed: " + maxContractsAllowed + "volumeSizing: " + volumeSizing);
		
			maxContractsAllowed = Math.Min(maxContractsAllowed, (int)Math.Ceiling(0.05d * currentVolume));	// 5. Liquidity Sizing
			
			return maxContractsAllowed;
		}
		
		protected bool checkEntryTimings(int[] cells,int version, bool isR2){
			string vTiming = "V" + version;
			if (isR2)
				vTiming = "V0";
			if (version != 4 && VTimingDic.ContainsKey(vTiming))
				return checkWithinTimeRange(cells, vTiming);
			else if (version == 4){
				if (VTimingDic.ContainsKey(vTiming + "A") && VTimingDic.ContainsKey(vTiming + "B"))
					return checkWithinTimeRange(cells, vTiming + "A") || checkWithinTimeRange(cells, vTiming + "B");
				}
				return false;
		}
		
		long SStart = 0;
		long CCutOff = 0;
		bool hasDayLightSavings = false;
		int typeSegIndex = 1;
		
		public bool checkWithinTimeRange(int[] cells, string vName){
			long Start = ToTime(VTimingDic[vName].start);
			long CutOff = ToTime(VTimingDic[vName].cutOff);
			
			hasDayLightSavings = daylightsavings();
			
			if (!daylightsavings()){
				Start = ToTime(VTimingDic[vName].start.AddHours(1));
				CutOff = ToTime(VTimingDic[vName].cutOff.AddHours(1));
			}
			
			if (this.CheckIsHoliday(Time[cells[cells.Length -1]])) // Check within Hol time Range
				return false;
			
			SStart = Start;
			CCutOff = CutOff;
		
			if (Start < CutOff){	// Within 1 day Active 	//as long as 12am dont fall within active time span	
				if (ToTime(Time[cells[cells.Length - 1]]) >= 240000 - (cells.Length - 1) * 100)
					Start += 240000;
							
				typeSegIndex = 1; 
				return (Start <= ToTime(Time[cells[cells.Length - 1]]) && ToTime(Time[0]) <= CutOff);	//eTime, then stop etime should be start cash time; 
			}
			else{	// cash time	//and Intra Day Active	//all v5, 
				typeSegIndex = 2;
				return (Start <= ToTime(Time[0]) && ToTime(Time[0]) <= 240000) || (0 <= ToTime(Time[0]) && ToTime(Time[0]) <= CutOff);	
			}
			return false;
		}
		
		public int getDirection(bool isLong){
			if (isLong)
				return 1;
			else
				return -1;
		}
		
	//	#region Omit Lists
		List<int> S0_OmitList = null;	// Section0
		public bool Section0 (int[] cells, int[] omitIndex, int version, bool isLong, bool isR2){
		bool result = true;
		
		if (isR2)
			return result;
		
		S0_OmitList = LoadOmitList (S0_OmitList, omitIndex); //.. MIRROR IMAGE		//time
			if (S0_OmitList.Contains(version))
				return true;
			result &= ToTime(Time[29]) == ToTime(Time[0].AddMinutes(-29));
			result &= ToTime(Time[29 + cells[cells.Length - 1]]) == ToTime(Time[cells[cells.Length - 1]].AddMinutes(-29));
			result &= ((indicator == 0) || ( indicator == 1 && ( (isLong && (Position.MarketPosition == MarketPosition.Long )) || (!isLong && (Position.MarketPosition == MarketPosition.Short )))  ));
			
			result &= (BarsSinceExit() >= 0 || BarsSinceExit() == -1);
			return result;
		}

		List<int> S00_OmitList = null;	// Section00 .. MIRROR IMAGE			//adx && dm
		/// <summary>
		/// For R1 and R2
		/// </summary>
		public bool Section00 (int[] cells, int[] omitIndex, int version, bool isLong, bool isR2){
		bool result = true;
		
		double TickSizeValS1 = 0;
			if (version >= 4)
				TickSizeValS1 = 1;
		
		S00_OmitList = LoadOmitList (S00_OmitList, omitIndex);
			if (S00_OmitList.Contains(version))
				return true;
				
			if (isLong){
				result &= DoRoundNoMult(DM(14).DiPlus[0]) - DoRoundNoMult(DM(14).DiMinus[0]) > 0;
				result &= DoRoundNoMult(DM(14).DiPlus[0]) - DoRoundNoMult(ADX(14)[0]) >= 0;	
				result &= DoRoundNoMult(ADX(14)[0]) - DoRoundNoMult(DM(14).DiMinus[0]) >= 0;	
			}
			else{
				result &= DoRoundNoMult(DM(14).DiPlus[0]) - DoRoundNoMult(DM(14).DiMinus[0]) < 0;
				result &= DoRoundNoMult(DM(14).DiMinus[0]) - DoRoundNoMult(ADX(14)[0]) >= 0;	
				result &= DoRoundNoMult(ADX(14)[0]) - DoRoundNoMult(DM(14).DiPlus[0]) >= 0;	
			}
			
			for (int i = 0; i < cells.Length - 1; i++)		
				result &= (DoRoundNoMult(ADX(14)[cells[i]]) - DoRoundNoMult(ADX(14)[cells[i+1]]) >= 0);
			
			result &= !isR2 ? (DoRoundNoMult(ADX(14)[0] - ADX(14)[cells[cells.Length - 1]]) > TickSizeValS1 + Convert.ToInt32(cells[cells.Length - 1])) : 
							  (DoRoundNoMult(ADX(14)[0] - ADX(14)[cells[cells.Length - 1]]) >= TickSizeValS1 + Convert.ToInt32(cells[cells.Length - 1]));
			return result;
		}	

		List<int> S1_OmitList = null;	// Section1 .. MIRROR IMAGE			//atr
		/// <summary>
		/// For R1 and R2.
		/// </summary>
		public bool Section1 (int[] cells, int[] omitIndex, int version, bool isLong, bool isR2){
		bool result = true;
		S1_OmitList = LoadOmitList (S1_OmitList, omitIndex);	
			if (S1_OmitList.Contains(version))
				return true;
			
			for (int i = 0; i < cells.Length; i++){
				result &= (DoRoundMult(ATR(14)[cells[i]]) >= 1);
				result &= (DoRoundMult(Range()[cells[i]]) >= DoRoundMult(ATR(14)[cells[i]]));	
				if (isLong){
						//result &= DoRoundMult(Close[cells[cells.Length - 1]]) - DoRoundMult(Bollinger(2,14).Middle[cells[cells.Length - 1]]) > 0;				
					result &= (2 * (DoRoundMult(Close[cells[i]]) - DoRoundMult(Open[cells[i]])) < 7 * DoRoundMult(ATR(14)[cells[i]]));
					result &= (2 * (DoRoundMult(Close[cells[cells.Length - 1] + 1]) - DoRoundMult(Open[cells[cells.Length - 1] + 1])) < 7 * DoRoundMult(ATR(14)[cells[cells.Length - 1] + 1]));
					if (version >= 5)
//						result &= (DoRoundMult(Open[0]) - DoRoundMult(EMA(3)[0]) < 0);
						result &= (  DoRoundMult(Close[0]) >= DoRoundMult(Bollinger(2,14).Upper[0]) ) ? (DoRoundMult(Open[0]) <= DoRoundMult(EMA(3)[0])) : (DoRoundMult(Open[0]) < DoRoundMult(EMA(3)[0]) );
				}
				else{
						//result &= DoRoundMult(Close[cells[cells.Length - 1]]) - DoRoundMult(Bollinger(2,14).Middle[cells[cells.Length - 1]]) < 0;			
					result &= (2* (DoRoundMult(Open[cells[i]]) - DoRoundMult(Close[cells[i]])) < 7 * DoRoundMult(ATR(14)[cells[i]]));
					result &= (2 * (DoRoundMult(Open[cells[cells.Length - 1] + 1]) - DoRoundMult(Close[cells[cells.Length - 1] + 1])) < 7 * DoRoundMult(ATR(14)[cells[cells.Length - 1] + 1]));
					if (version >= 5)
//						result &= (DoRoundMult(Open[0]) - DoRoundMult(EMA(3)[0]) > 0);
						result &= ( DoRoundMult(Close[0]) <= DoRoundMult(Bollinger(2,14).Lower[0]) ) ? (DoRoundMult(Open[0]) >= DoRoundMult(EMA(3)[0])) : (DoRoundMult(Open[0]) > DoRoundMult(EMA(3)[0]) );
				}
			}

			for (int i = 1; i < 2; i++){    //handles just 0 > cells[1]
				if (isLong && (DoRoundMult(Close[0]) - DoRoundMult(Open[0]) > 2))
					result &= ( 4 * (DoRoundMult(Close[0]) - DoRoundMult(Open[0])) >= (DoRoundMult(Close[cells[i]]) - DoRoundMult(Open[cells[i]]))); 
				else if(!isLong && (DoRoundMult(Close[0]) - DoRoundMult(Open[0]) < -2))
					result &= ( 4 * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) >= (DoRoundMult(Open[cells[i]]) - DoRoundMult(Close[cells[i]])));  
			}
			//all are important; dont change anything in the 5 lines
			result &= DoRoundMult(ATR(14)[0] - ATR(14)[cells[cells.Length - 1] + 1]) <= DoRoundMult(ATR(14)[cells[cells.Length - 1]] - ATR(14)[cells[cells.Length - 1] + 1]) + Convert.ToInt32(cells[cells.Length - 1]) ;
			result &= DoRoundMult(ATR(14)[cells[cells.Length - 1]] - ATR(14)[cells[cells.Length - 1] + 1]) <= DoRoundMult(ATR(14)[0] - ATR(14)[cells[cells.Length - 1] + 1]);
			result &= version <= 1 ? DoRoundMult(ATR(14)[0] - ATR(14)[cells[cells.Length - 1] + 1]) >= 0 : DoRoundMult(ATR(14)[0] - ATR(14)[cells[cells.Length - 1] + 1]) >= 1; // v5 is 1 not 2, v4 is 1, ok for v123 //dont change drm order	
			result &= DoRoundNoMult(DoRoundMult(ATR(14)[cells[cells.Length - 1]]) - DoRoundMult(ATR(14)[cells[cells.Length - 1] + 1])) >= 0;
			result &= DoRoundNoMult(DoRoundMult(ATR(14)[cells[cells.Length - 1]]) - DoRoundMult(ATR(14)[cells[cells.Length - 1] + 1])) <= 2;
			result &= DoRoundMult(ATR(14)[0]) >= 2 && DoRoundMult(Range()[0]) >= 3; //range?!
				
			return result;
		}
		
		List<int> S2_OmitList = null;	// Section2 ..NOT MIRROR IMAGE
		public bool Section2 (int[] cells, int[] omitIndex, int version, bool isLong, bool isR2){
		bool result = true;
					
		if (isR2)
			return result;
		
		S2_OmitList = LoadOmitList (S2_OmitList, omitIndex);
			if (S2_OmitList.Contains(version))
				return true;
			
			double TickSizeVal2 = 0;
			double TickSizeVal3 = 1;

			if (version >= 4)
				TickSizeVal2 = -1;	//looser better // don't change
				TickSizeVal3 = 0;	//stricter better // don't change
			
			for (int i = 0; i < cells.Length - 1; i++){
				if (isLong){
					result &=(DoRoundMult(Bollinger(2,14).Upper[cells[i]]) - DoRoundMult(Bollinger(2,14).Middle[cells[i]]) > DoRoundNoMult(longBollAvg[cells[i+1]]) - 2);//
					result &= DoRoundMult(Bollinger(2,14).Upper[cells[i]]) - DoRoundMult(Bollinger(2,14).Middle[cells[i]]) > 
							  DoRoundMult(Bollinger(2,14).Upper[cells[i+1]]) - DoRoundMult(Bollinger(2,14).Middle[cells[i+1]]) + TickSizeVal2;
					result &= DoRoundMult(Bollinger(2,14).Upper[cells[cells.Length - 1] + 1]) - DoRoundMult(Bollinger(2,14).Middle[cells[cells.Length - 1] + 1]) + TickSizeVal3 <= 
							  DoRoundMult(Bollinger(2,14).Upper[cells[cells.Length - 1]]) - DoRoundMult(Bollinger(2,14).Middle[cells[cells.Length - 1]]);					
				}
				else{	
					result &=(DoRoundMult(Bollinger(2,14).Middle[cells[i]]) - DoRoundMult(Bollinger(2,14).Lower[cells[i]]) > DoRoundNoMult(shortBollAvg[cells[i+1]]) - 2);
					result &= DoRoundMult(Bollinger(2,14).Middle[cells[i]]) - DoRoundMult(Bollinger(2,14).Lower[cells[i]]) > 
							  DoRoundMult(Bollinger(2,14).Middle[cells[i+1]]) - DoRoundMult(Bollinger(2,14).Lower[cells[i+1]]) + TickSizeVal2;
					result &= DoRoundMult(Bollinger(2,14).Middle[cells[cells.Length - 1] + 1]) - DoRoundMult(Bollinger(2,14).Lower[cells[cells.Length - 1] + 1]) + TickSizeVal3 <= 
							  DoRoundMult(Bollinger(2,14).Middle[cells[cells.Length - 1]]) - DoRoundMult(Bollinger(2,14).Lower[cells[cells.Length - 1]]);
				}
			}
			return result;		
		}
		
		List<int> S3_OmitList = null;	// Section 3 ..NOT MIRROR IMAGE
		public bool Section3 (int[] cells, int[] omitIndex, int version, bool isLong, bool isR2){
		bool result = true;
					
		if (isR2)
			return result;
		
		S3_OmitList = LoadOmitList (S3_OmitList, omitIndex);
			if (S3_OmitList.Contains(version))
				return true;
			for (int i = 0; i < cells.Length; i++){
				if (isLong)
					result &= (DoRoundMult(Close[cells[i]]) - DoRoundMult(Bollinger(2,14).Upper[cells[i]]) >= 0);
				else
					result &= (DoRoundMult(Close[cells[i]]) - DoRoundMult(Bollinger(2,14).Lower[cells[i]]) <= 0);
			}
			for (int i = 0; i < cells.Length - 1; i++){	
				if (isLong){
					result &= DoRoundMult(Low[cells[i]]) >= DoRoundMult(Low[cells[i + 1]]) - 1;  //NEW
					result &= DoRoundMult(High[cells[i]]) >= DoRoundMult(High[cells[i + 1]]) - 1;  //NEW
				}
				else{
					result &= DoRoundMult(High[cells[i]]) <= DoRoundMult(High[cells[i + 1]]) + 1;  //NEW
					result &= DoRoundMult(Low[cells[i]]) <= DoRoundMult(Low[cells[i + 1]]) + 1;  //NEW
				}
			}
			return result;		
		}		

		List<int> S5_OmitList = null;	// Section 5 .. MIRROR IMAGE
		/// <summary>
		/// For R1 and R2 but needs some alteration.
		/// </summary>
		public bool Section5 (int[] cells, int[] omitIndex, int version, bool isLong, bool isR2){
		bool result = true;
		S5_OmitList = LoadOmitList (S5_OmitList, omitIndex);
			if (S5_OmitList.Contains(version))
				return true;

			int rVariable = 1;
			if (isR2)
				rVariable = 2;
				
			for (int i = 1; i < cells.Length - 1; i++){	//else just plain cell.Length, wo the -1
				result &= (DoRoundMult(Range()[cells[i]]) >= -rVariable + DoRoundMult(Range()[0]) - DoRoundMult(ATR(14)[0]));
				result &= (DoRoundMult(Range()[cells[i]]) <=  rVariable + DoRoundMult(Range()[0]) + DoRoundMult(ATR(14)[0]));
			}
			
			for (int i = cells.Length - 1; i < cells.Length; i++){	// Do for Last
				if ((!isLong && DoRoundMult(Open[0]) + 2 < DoRoundMult(EMA(3)[0])) || (isLong && DoRoundMult(Open[0]) - 2 > DoRoundMult(EMA(3)[0]))){
					result &= (DoRoundMult(Range()[cells[i]]) >= -rVariable + DoRoundMult(Range()[0]) - DoRoundMult(ATR(14)[0]));
					result &= (DoRoundMult(Range()[cells[i]]) <= 1 + rVariable + DoRoundMult(Range()[0]) + DoRoundMult(ATR(14)[0]));				
				}
				else{
					result &= (DoRoundMult(Range()[cells[i]]) >= -rVariable + DoRoundMult(Range()[0]) - DoRoundMult(ATR(14)[0]));
					result &= (DoRoundMult(Range()[cells[i]]) <=  rVariable + DoRoundMult(Range()[0]) + DoRoundMult(ATR(14)[0]));
				}
			}
			
//			if ((version ==1 || version ==2) && DoRoundNoMult(ATR(14)[0]/ATR(14)[cells[cells.Length - 1]+1]) == 2){	//422; c==2
//				result &= DoRoundNoMult((High[cells[cells.Length - 1]] - Low[cells[cells.Length - 1]])/(ATR(14)[cells[cells.Length - 1]+1])) - //a-c<=1
//						  DoRoundNoMult(ATR(14)[0]/ATR(14)[cells[cells.Length - 1]+1]) <= 1;	
//			}		
//			if ( DoRoundNoMult(ATR(14)[0]/ATR(14)[cells[cells.Length - 1]+1]) == 2){	//522; c==2
//				result &= DoRoundNoMult((High[cells[cells.Length - 1]] - Low[cells[cells.Length - 1]])/(ATR(14)[cells[cells.Length - 1]+1])) <=4;	//a-c<=4
//			}
//								
//			if (DoRoundNoMult((High[cells[cells.Length - 1]] - Low[cells[cells.Length - 1]])/(2*(ATR(14)[cells[cells.Length - 1]+1]))) > 0 &&	//312; remove 45 b>0, c>b	 
//				DoRoundNoMult(ATR(14)[0]/ATR(14)[cells[cells.Length - 1]+1]) > 
//				DoRoundNoMult((High[cells[cells.Length - 1]] - Low[cells[cells.Length - 1]])/(2*(ATR(14)[cells[cells.Length - 1]+1])))){
//				result &= version <= 3; 
//			}
//					
//			if (DoRoundNoMult((High[cells[cells.Length - 1]] - Low[cells[cells.Length - 1]])/(2*(ATR(14)[cells[cells.Length - 1]+1]))) == 0 &&  //101; remove 123 a==c, b==0
//				DoRoundNoMult((High[cells[cells.Length - 1]] - Low[cells[cells.Length - 1]])/(ATR(14)[cells[cells.Length - 1]+1])) == 
//				DoRoundNoMult(ATR(14)[0]/ATR(14)[cells[cells.Length - 1]+1])){ 
//				 result &= version >= 4 || version < 1;	
//			}
			
			return result;		
		}		
		
		List<int> S7_OmitList = null;	// Section 7 ..NOT MIRROR IMAGE
		public bool Section7 (int[] cells, int[] omitIndex, int version, bool isLong, bool isR2){	
		bool result = true;
						
		if (isR2)
			return result;
		
		S7_OmitList = LoadOmitList (S7_OmitList, omitIndex);
			if (S7_OmitList.Contains(version))
				return true;
			result &= isLong ? INtrendCrossLong : INtrendCrossShort;
			return result;
		}    
		
		List<int> S8_OmitList = null;	// Section 8 ..IS MIRROR IMAGE
		/// <summary>
		/// For R1 and R2 but needs some alteration.
		/// </summary>
		public bool Section8 (int[] cells, int[] omitIndex, int version, bool isLong, bool isR2){
		bool result = true;
		S8_OmitList = LoadOmitList (S8_OmitList, omitIndex);
			if (S8_OmitList.Contains(version))
				return true;
			for (int i = 0; i < cells.Length; i++){
				if (isLong){
					if (!isR2){
						result &= (DoRoundMult(Open[cells[i]]) <= DoRoundMult(Close[cells[i]]));
						result &= (DoRoundMult(EMA(3)[cells[cells.Length - 1]]) - DoRoundMult(EMA(35)[cells[cells.Length - 1]]) >= -1);
							//result &= (DoRoundMult(Open[cells[i]]) - DoRoundMult(EMA(3)[cells[i]]) < 1);	
						result &= (DoRoundMult(Low[cells[cells.Length - 1]]) > DoRoundMult(Bollinger(2,14).Lower[cells[cells.Length - 1]]));	//NEW	
							result &= version >= 4 ? DoRoundMult(Range()[0]) < 8* DoRoundMult(Range()[cells[cells.Length - 1] + 1]) : DoRoundMult(Range()[0]) < 7* DoRoundMult(Range()[cells[cells.Length - 1] + 1]);
					}
					else	//r2
						result &= (DoRoundMult(Open[cells[i]]) < DoRoundMult(Close[cells[i]]));			
						result &= (DoRoundMult(Close[cells[i]]) - DoRoundMult(EMA(3)[cells[i]]) > 0);
						result &= DoRoundMult(Low[cells[cells.Length - 1] + 1]) >= DoRoundMult(Bollinger(2,14).Lower[cells[cells.Length - 1] + 1]) - 1;//r2?							
				}
				else{
					if (!isR2){
						result &= (DoRoundMult(Open[cells[i]]) >= DoRoundMult(Close[cells[i]]));
						result &= (DoRoundMult(EMA(3)[cells[cells.Length - 1]]) - DoRoundMult(EMA(35)[cells[cells.Length - 1]]) <= 1);		
							//result &= (DoRoundMult(Open[cells[i]]) - DoRoundMult(EMA(3)[cells[i]]) > -1);	
						result &= (DoRoundMult(High[cells[cells.Length - 1]]) < DoRoundMult(Bollinger(2,14).Upper[cells[cells.Length - 1]]));	//NEW 	
							result &= version >= 4 ? DoRoundMult(Range()[0]) < 8* DoRoundMult(Range()[cells[cells.Length - 1] + 1]) : DoRoundMult(Range()[0]) < 7* DoRoundMult(Range()[cells[cells.Length - 1] + 1]);							
					}
					else
						result &= (DoRoundMult(Open[cells[i]]) > DoRoundMult(Close[cells[i]]));
						result &= (DoRoundMult(Close[cells[i]]) - DoRoundMult(EMA(3)[cells[i]]) < 0);	
						result &= DoRoundMult(High[cells[cells.Length - 1] + 1]) <= DoRoundMult(Bollinger(2,14).Upper[cells[cells.Length - 1] + 1]) + 1;							
				}
			}
			return result;	
		}

		List<int> S9_OmitList = null;	// Section 9 ..ALMOST MIRROR IMAGE
		/// <summary>
		/// For R1 and R2.
		/// </summary>
		public bool Section9 (int[] cells, int[] omitIndex, int version, bool isLong, bool isR2){
		bool result = true;
		S9_OmitList = LoadOmitList (S9_OmitList, omitIndex);
			if (S9_OmitList.Contains(version))
				return true;
			if (isLong){
				for (int i = 0; i < cells.Length - 1; i++)
					if (isR2)
						result &= DoRoundMult(Close[cells[i]]) > DoRoundMult(Close[cells[i + 1]]);  
					else
						result &= DoRoundMult(Close[cells[i]]) >= DoRoundMult(Close[cells[i + 1]]) + (cells[i + 1] - cells[i] - 1); //!!??			
					result &= DoRoundMult(Close[0]) > DoRoundMult(Close[cells[cells.Length - 1]]) + cells[cells.Length - 1];
				
				if (DoRoundMult(Open[0]) == DoRoundMult(Close[0])  )
					result &= DoRoundMult(Close[0]) > DoRoundMult(Close[cells[1]]);// + 1; 
				
				if (version >= 4)
					for (int i = 2; i < version; i++){
						result &= DoRoundMult(Close[i - 2]) >= DoRoundMult(Close[i]) + 2; //see if can fit into v123
					}
					 
//				if (version == 2 && DoRoundMult(Open[2]) < DoRoundMult(Close[2]))
//					result &= DoRoundMult(Range()[0]) + DoRoundMult(ATR(14)[0]) <= DoRoundMult(Open[2])- DoRoundMult(Close[2]);
//				if (version == 3 && DoRoundMult(Open[1]) < DoRoundMult(Close[1]))
//					result &= DoRoundMult(Range()[0]) + DoRoundMult(ATR(14)[0]) <= DoRoundMult(Open[1])- DoRoundMult(Close[1]);
			}
			else{
				for (int i = 0; i < cells.Length - 1; i++)
					if (isR2)
						result &= DoRoundMult(Close[cells[i]]) < DoRoundMult(Close[cells[i + 1]]);  
					else
						result &= DoRoundMult(Close[cells[i]]) <= DoRoundMult(Close[cells[i + 1]]) - (cells[i + 1] - cells[i] - 1); 
					result &= DoRoundMult(Close[0]) < DoRoundMult(Close[cells[cells.Length - 1]]) -  cells[cells.Length - 1];

				if (DoRoundMult(Open[0]) == DoRoundMult(Close[0]) )
					result &= DoRoundMult(Close[0]) < DoRoundMult(Close[cells[1]]);// - 1; 
			
				if (version >= 4)
					for (int i = 2; i < version; i++){
						result &= DoRoundMult(Close[i - 2]) <= DoRoundMult(Close[i]) - 2; //see if can fit into v123
					}
//				if (version == 2 && DoRoundMult(Open[2]) > DoRoundMult(Close[2]))
//					result &= DoRoundMult(Range()[0]) + DoRoundMult(ATR(14)[0]) >= DoRoundMult(Open[2])- DoRoundMult(Close[2]);
//				if (version == 3 && DoRoundMult(Open[1]) > DoRoundMult(Close[1]))
//					result &= DoRoundMult(Range()[0]) + DoRoundMult(ATR(14)[0]) >= DoRoundMult(Open[1])- DoRoundMult(Close[1]);
			}
			
//			if (version == 5)
//				result &= 2 * DoRoundNoMult(ADX(14)[5]) < DoRoundNoMult(1 + ((DM(14).DiMinus[5] + DM(14).DiPlus[5])));
			
			return result;	
		}

		List<int> S10_OmitList = null;	// Section 10 ..IS MIRROR IMAGE
		/// <summary>
		/// For R1 & R2
		/// </summary>
		public bool Section10 (int[] cells, int[] omitIndex, int version, bool isLong, bool isR2){
		bool result = true;
		S10_OmitList = LoadOmitList (S10_OmitList, omitIndex);
			if (S10_OmitList.Contains(version))
				return true;
			for (int i = 0; i < cells[cells.Length - 1]; i++){	//handles the jumps
				if (isLong)
					result &= DoRoundMult(Close[i + 1]) + 3 >= DoRoundMult(Open[i]);
				else
					result &= DoRoundMult(Close[i + 1]) - 3 <= DoRoundMult(Open[i]);
			}
			return result;	
		}
		
		List<int> S11_OmitList = null;	// Section 11 ..ALMOST MIRROR IMAGE
		/// <summary>
		/// For R1 & R2
		/// </summary>
		public bool Section11 (int[] cells, int[] omitIndex, int version, bool isLong, bool isR2){
		bool result = true;	
			
		if (isR2)
			return result;
		
		S11_OmitList = LoadOmitList (S11_OmitList, omitIndex);
			if (S11_OmitList.Contains(version))
				return true;

			if (isLong){
					result &= DoRoundMult(Range()[0]) < (4 + Convert.ToInt32(cells[cells.Length - 1])) * DoRoundMult(Range()[cells[cells.Length - 1] + 1]) -1 ;
				//result &= (DoRoundMult(Close[cells[cells.Length - 1]]) - DoRoundMult(Open[cells[cells.Length - 1]])) <= 2*(DoRoundMult(Close[0]) - DoRoundMult(Open[0]));
		//		result &= DoRoundMult(Close[cells[cells.Length - 1] + 1]) > DoRoundMult(Open[cells[cells.Length - 1] + 1]) ? DoRoundMult(Close[cells[cells.Length - 1] + 1]) >  DoRoundMult(Bollinger(2,14).Middle[cells[cells.Length - 1] + 1]) : true ;
//					result &= (DoRoundMult(Bollinger(2,14).Upper[0]) - DoRoundMult(Bollinger(2,14).Middle[0])) <= 
//						1 + (1 + Convert.ToInt32(cells[cells.Length - 1])) * (DoRoundMult(Bollinger(2,14).Upper[cells[cells.Length - 1] + 1]) - DoRoundMult(Bollinger(2,14).Middle[cells[cells.Length - 1] + 1]));
					result &= DoRoundMult(Open[cells[cells.Length - 1] ]) <= DoRoundMult(EMA(3)[cells[cells.Length - 1]]) + 1;
				result &= (DoRoundMult(High[cells[cells.Length - 1] + 1]) <= DoRoundMult(High[cells[cells.Length - 1]]) || DoRoundMult(Range()[cells[cells.Length - 1] + 1]) <= 1); 
					result &= (version >= 4 ? DoRoundMult(Low[cells[cells.Length - 2] ]) > DoRoundMult(Low[cells[cells.Length - 1]]) : true);
				
				result &= (version >= 4 ? 5 * (DoRoundMult(Close[0]) - DoRoundMult(Open[0])) >= DoRoundMult(Range()[0]) : 
							(DoRoundMult(Open[0]) < DoRoundMult(Close[0]) ? (5 * (DoRoundMult(Close[0]) - DoRoundMult(Open[0])) > DoRoundMult(Range()[0]) || DoRoundMult(Range()[0]) <= 5) : true));
							
				result &= version >= 4 ? -2 + DoRoundMult(High[cells[cells.Length - 1] + 1]) <= DoRoundMult(High[cells[cells.Length - 1]]) : DoRoundMult(High[cells[cells.Length - 1] + 1]) <= DoRoundMult(High[cells[cells.Length - 1]]);
				
				result &= 2 * DoRoundMult(Range()[cells[cells.Length - 1]]) > DoRoundMult(Range()[cells[cells.Length - 1] + 1]);//bigger
				result &= ((DoRoundMult(Range()[cells[cells.Length - 1]]) <= 3 + 4 * (1 + DoRoundMult(Range()[cells[cells.Length - 1] + 1]))
							&& DoRoundMult(Range()[cells[cells.Length - 1] + 1]) > 1  )
							||
							(DoRoundMult(Range()[cells[cells.Length - 1] ]) <= 3*DoRoundMult(Range()[cells[cells.Length - 1] + 1])
							&& DoRoundMult(Range()[cells[cells.Length - 1] + 1]) >= 1 )
				)
				;//smaller
				result &= DoRoundNoMult(DoRoundMult(High[0]) - DoRoundMult(High[1])) >= -1;	//best way to include v3, all other v are accounted for
			//	result &= 2 * DoRoundNoMult(DoRoundMult(Close[0]) - DoRoundMult(Low[0])) >= DoRoundMult(Range()[0]);	
			}	
			else{	
					result &= DoRoundMult(Range()[0]) < (4 + Convert.ToInt32(cells[cells.Length - 1])) * DoRoundMult(Range()[cells[cells.Length - 1] + 1]) -1 ;
			//	result &= (DoRoundMult(Open[cells[cells.Length - 1]]) - DoRoundMult(Close[cells[cells.Length - 1]])) <= 2*(DoRoundMult(Open[0]) - DoRoundMult(Close[0]));
			//	result &= DoRoundMult(Close[cells[cells.Length - 1] + 1]) < DoRoundMult(Open[cells[cells.Length - 1] + 1]) ? DoRoundMult(Close[cells[cells.Length - 1] + 1]) <  DoRoundMult(Bollinger(2,14).Middle[cells[cells.Length - 1] + 1]) : true ;
					//result &= (DoRoundMult(Close[0]) + DoRoundMult(ATR(14)[0]) >= DoRoundMult(Close[cells[cells.Length - 1]]) - 2 * Convert.ToInt32(celltwo) ) ;   
					result &= DoRoundMult(Open[cells[cells.Length - 1] ]) >= DoRoundMult(EMA(3)[cells[cells.Length - 1]]) - 1;
				result &= (DoRoundMult(Low[cells[cells.Length - 1] + 1]) >= DoRoundMult(Low[cells[cells.Length - 1]]) || DoRoundMult(Range()[cells[cells.Length - 1] + 1]) <= 1); 
					result &= (version >= 4 ? DoRoundMult(High[cells[cells.Length - 2] ]) < DoRoundMult(High[cells[cells.Length - 1]]) : true);
				
				result &= (version >= 4 ? 5 * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) >= DoRoundMult(Range()[0]) : 
							(DoRoundMult(Open[0]) > DoRoundMult(Close[0]) ? (5 * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) > DoRoundMult(Range()[0]) || DoRoundMult(Range()[0]) <= 5) : true));
				
				result &= version >= 4 ? 2 + DoRoundMult(Low[cells[cells.Length - 1] + 1]) >= DoRoundMult(Low[cells[cells.Length - 1]]) : DoRoundMult(Low[cells[cells.Length - 1] + 1]) >= DoRoundMult(Low[cells[cells.Length - 1]]);
				
				result &= 2 * DoRoundMult(Range()[cells[cells.Length - 1]]) > DoRoundMult(Range()[cells[cells.Length - 1] + 1]);
				result &= ((DoRoundMult(Range()[cells[cells.Length - 1]]) <= 3 + 4 * (1 + DoRoundMult(Range()[cells[cells.Length - 1] + 1]))
							&& DoRoundMult(Range()[cells[cells.Length - 1] + 1]) > 1 ) 
							||
							(DoRoundMult(Range()[cells[cells.Length - 1] ]) <= 3*DoRoundMult(Range()[cells[cells.Length - 1] + 1])
							&& DoRoundMult(Range()[cells[cells.Length - 1] + 1]) >= 1 )// here also
				)
				;
				result &= DoRoundNoMult(DoRoundMult(Low[0]) - DoRoundMult(Low[1])) <= 1; 	//best way to include v3, all other v are accounted for		
			//	result &= 2 * DoRoundNoMult(DoRoundMult(High[0]) - DoRoundMult(Close[0])) >= DoRoundMult(Range()[0]);	 
			}
			return result;	
		}
	
		public List<int> LoadOmitList (List<int> OmitList,int[] omitIndex){	// Load OmitList
			if (OmitList == null){
				OmitList = new List<int>();
				for (int i = 0; i < omitIndex.Length; i++)
					OmitList.Add(omitIndex[i]);	
			}
			return OmitList;
		}
			
		bool exitbegun = false;
		protected void testMALongExits(bool isLong, bool isR2){
			DirectionType directionType = isLong ? DirectionType.L:DirectionType.S ;
			PhaseType phaseType = PhaseType.X ;

			exitbegun = zStop(1, 2, 1, isLong, isR2);	
				if (!exitbegun){
					exitbegun = zStop(1, 3, 2, isLong, isR2);	
				}
				if (!exitbegun){
					exitbegun = zStop(2, 3, 3, isLong, isR2);	
				}		
				
							
//				if ( this.getRules().doEXIT_RULE_A	)
//					if (this.getRules().EXIT_RULE_A( isLong ))
//					{
//						Print("EXIT A EXIT A EXIT A EXIT A EXIT A ");
//						//int LookBackBars = 3;
//						if (/*EXIT_TIME_BREAK_CHECKER (LookBackBars) && (*/isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short)//)
//							getOut(get ExitLabel(directionType, phaseType, ExitLetterType.A, isR2), isLong);
//					}
//				
//				if ( this.getRules().doEXIT_RULE_B	)
//					if (this.getRules().EXIT_RULE_B( isLong ))
//					{
//						//int LookBackBars = 2;
//						if (/*EXIT_TIME_BREAK_CHECKER (LookBackBars) && (*/isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short)//)
//							{
//							if (!isR2 && BarsSinceEntry() >= 1){
//								getOut(getExitLabel(directionType, phaseType, ExitLetterType.B, isR2), isLong);
//							}
//							else if (isR2 && BarsSinceEntry(R2signal) >= 1){
//								getOut(getExitLabel(directionType, phaseType, ExitLetterType.B, isR2), isLong);
//							}
//						}
//					}
//			if ( this.getRules().doEXIT_RULE_U	)
//				if (this.getRules().EXIT_RULE_U( flattenclgcsi, flatten2clgcsi, flattenfd, flatten2fd, flatten6e, flatten26e ))
//				{
//					//int LookBackBars = 0;	//this .U don't need...
//					if (/*EXIT_TIME_BREAK_CHECKER (LookBackBars) && (*/isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short)//)
//						getOut(getExitLabel(directionType, phaseType, ExitLetterType.U, isR2), isLong);
//				}
	
		if (!exitbegun
				&& BarsSinceEntry() >= 2
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])
				&& DoRoundMult(Range()[0]) > DoRoundMult(Range()[2]) + 1	
				&& DoRoundNoMult(ADX(14)[0]) - getUniqueFunctions(UniqueFunctionType.DI, isLong, new String[]{"0"}) > 2	
				
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[0])) <= 0
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Open[1])) <= 0
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Open[2])) <= 0
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Close[1])) <= -1
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Close[2])) <= -1

				&& getDirection(isLong) * (DoRoundNoMult(DM(14).DiMinus[0]) - DoRoundNoMult(DM(14).DiPlus[0])) >= 1
				&& getDirection(isLong) * (DoRoundNoMult(DM(14).DiMinus[1]) - DoRoundNoMult(DM(14).DiPlus[1])) >= 1
				&& getDirection(isLong) * (DoRoundNoMult(DM(14).DiMinus[2]) - DoRoundNoMult(DM(14).DiPlus[2])) >= 1
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[2]) - DoRoundMult(EMA(35)[2])) <= 0
				&& getDirection(isLong) * (DoRoundMult(EMA(14)[1]) - DoRoundMult(EMA(35)[1])) <= 0
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","0"})) < 0 	
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","1"})) <= 0	
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","2"})) < 0	
			){
				int LookBackBars = 4;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.A, isR2), isLong);
			}
		if (!exitbegun	
				&& ((getDirection(isLong) * DoRoundMult(Close[0]) < getDirection(isLong) * DoRoundMult(Open[BarsSinceEntry()]) - 1 && BarsSinceEntry() < 20)
					||
				   (getDirection(isLong) * DoRoundMult(Close[0]) > getDirection(isLong) * DoRoundMult(Open[BarsSinceEntry()]) + 1  //intentionally ignore >= -1 +...
					&& getUniqueFunctions(UniqueFunctionType.TWO14, isLong, null) > 0	//bse 14
					&& (DoRoundNoMult(ADX(14)[0]) - getUniqueFunctions(UniqueFunctionType.DI, isLong, new String[]{"0"}) >= 0 
						|| 
						DoRoundNoMult(ADX(14)[0]) - getUniqueFunctions(UniqueFunctionType.DI, !isLong, new String[]{"0"}) >= 0)
					)
				)
			
				&& (inR2 ? BarsSinceEntry(R2signal) > 1 : true)	
			
				&& ((BarsSinceEntry() >= 20 && getDirection(isLong) * DoRoundMult(Close[0]) > getDirection(isLong) * DoRoundMult(Open[BarsSinceEntry()]) + 1 && getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Open[2])) > 0 ) && 
			        getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Open[1])) > 2 ?
			          getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Open[1])) >= 2 * getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Open[2])) : true)
			
//				&& (BarsSinceEntry() >= 2 ? (getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Open[2])) >= 0 ?
//						getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Open[1])) >= getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Open[2])) :
//						getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Open[1])) >= getDirection(isLong) * (DoRoundMult(Open[2]) - DoRoundMult(Close[2]))) : true)
				
				&& (BarsSinceEntry() < 2 ? getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new string[]{"0"}) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","0"})) > 0 : true)
				
				&& DoRoundMult(Range()[1]) >= DoRoundMult(ATR(14)[1])
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Open[1])) > 0
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) > 2	//!!!
			
				&& ((BarsSinceEntry() >= 20 && getDirection(isLong) * DoRoundMult(Close[0]) > getDirection(isLong) * DoRoundMult(Open[BarsSinceEntry()]) + 1 && getDirection(isLong) * (DoRoundMult(Open[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","0"})) <= 1) ? (getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Bollinger(2,14).Middle[0])) >= -1) : true)			
				//&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[1])) < 0
				&& (BarsSinceEntry() < 20  ? getDirection(isLong) * (DoRoundMult(Open[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","0"})) >= 0 : true) 
				
				&& ((getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(EMA(3)[0])) <= -1 //impt, dont' change
					&& (BarsSinceEntry() >= 2  ? getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(EMA(3)[1])) <= -1 : true)) //!!!
					||
					(getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new string[]{"0"}) - getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new string[]{"1"})) > 0 //special case //h>h
					&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, !isLong, new string[]{"0"}) - getUniqueFunctions(UniqueFunctionType.HIGHLOW, !isLong, new string[]{"1"})) < 0 //l<l
					&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(EMA(3)[0])) >= 0 )) //impt, dont' change) 
									
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[1])) <= 1 
				&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new string[]{"0"}) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","0"})) >= 0 
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","1"})) >= 0	//dont change >= here!!	
				&& getDirection(isLong) * DoRoundMult(Open[0]) >= getDirection(isLong) * DoRoundMult(Open[BarsSinceEntry()])
					&& getDirection(isLong) * (DoRoundMult(Open[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","0"})) >= 0	
			){			
				int LookBackBars = 4;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short)){
					if ((!inR2 && BarsSinceEntry() >= 1) || (inR2 && BarsSinceEntry(R2signal) > 1))
						getOut(getExitLabel(directionType, phaseType, ExitLetterType.B, isR2), isLong);	
				}
			}
			if (!exitbegun
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])
				&& DoRoundMult(Range()[1]) >= DoRoundMult(ATR(14)[1])

				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(EMA(3)[1])) > 0
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(EMA(3)[2])) > 0 
				&& getDirection(isLong) * (DoRoundMult(Close[3]) - DoRoundMult(EMA(3)[3])) > 0 			
				&& getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Close[1])) < 0//CHECK <
				&& getDirection(isLong) * (DoRoundMult(Open[2]) - DoRoundMult(Close[2])) <= 0
				&& getDirection(isLong) * (DoRoundMult(Open[3]) - DoRoundMult(Close[3])) <= 0
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Close[2])) > 1
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Close[3])) > 1
				//&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[3])) <= 0//1 check
							
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","1"})) >= 0 
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[1])) <= 1
				
				&& (getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(EMA(3)[1])) <= 1 ? getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[3])) <= 0 : getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[3])) < 0 )
			){
				int LookBackBars = 5;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short)){
					if ((!inR2 && BarsSinceEntry() >= 4) || (inR2 && BarsSinceEntry(R2signal) >= 4))
						getOut(getExitLabel(directionType, phaseType, ExitLetterType.C, isR2), isLong);
				}
			}
			if (!exitbegun
				&& DoRoundMult(Range()[0]) > 1
				&& DoRoundMult(Range()[1]) > 1
				&& DoRoundMult(Range()[2]) > 1
				&& DoRoundNoMult(ADX(14)[0]) - DoRoundNoMult(ADX(14)[1]) > 0
				&& DoRoundNoMult(ADX(14)[1]) - DoRoundNoMult(ADX(14)[2]) >= 0//???
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])
				&& DoRoundMult(Range()[0]) >= DoRoundMult(Range()[1])
				&& DoRoundMult(Range()[1]) >= DoRoundMult(Range()[2])	//most important
				&& DoRoundMult(Range()[0]) >= DoRoundMult(Range()[2])
				
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Close[1])) < -1
				&& getDirection(isLong) * (DoRoundNoMult(DM(14).DiPlus[0]) - DoRoundNoMult(DM(14).DiMinus[0])) < -2  
					&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new string[]{"0"}) - getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new string[]{"1"})) <= 0
				
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(EMA(35)[0])) < getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(35)[1])) - 2 
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[2]) - DoRoundMult(EMA(35)[2])) < -1	//[2] > 0 !!!
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[2]) - DoRoundMult(EMA(35)[2])) < 0
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(35)[1])) <= -1
					//&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(35)[1])) < getDirection(isLong) * (DoRoundMult(EMA(3)[2]) - DoRoundMult(EMA(35)[2])) 

				&& DoRoundNoMult(ADX(14)[0]) - getUniqueFunctions(UniqueFunctionType.DI, isLong, new String[]{"0"}) < -2
			){
				int LookBackBars = 4;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.D, isR2), isLong); 
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 1
				&& DoRoundMult(ATR(14)[1]) > 1
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])
				&& DoRoundMult(Range()[1]) >= DoRoundMult(ATR(14)[1]) //+ 1
				&& DoRoundMult(Range()[0]) > DoRoundMult(Range()[1])	
				&& DoRoundMult(Range()[0]) <= 2 * DoRoundMult(Range()[1])	
				&& DoRoundNoMult(ADX(14)[0]) - DoRoundNoMult(ADX(14)[1]) >= 0	
				
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Close[1])) < 0
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Open[1])) < 0	
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[0])) < 0
				&& getDirection(isLong) * (DoRoundNoMult(DM(14).DiMinus[0]) - DoRoundNoMult(DM(14).DiPlus[0])) > 0
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) >= getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Close[1])) 
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","0"})) < 0 	
				&& getDirection(isLong) * (DoRoundMult(EMA(14)[0]) - DoRoundMult(EMA(35)[0])) <= 0
			){	
				int LookBackBars = 3;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short)){
					foreach (IOrder entryOrder in entryOrders.Values){
						string EntryName = entryOrder.Name;
						if ((EntryName.Contains("V1") && getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Open[BarsSinceEntry() + 3])) < -1) ||
							((EntryName.Contains("V2") || EntryName.Contains("V3") || EntryName.Contains("V4")) && getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Open[BarsSinceEntry() + 4])) < -1) ||
							(EntryName.Contains("V5") && getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Open[BarsSinceEntry() + 5])) < -1))
						getOut(getExitLabel(directionType, phaseType, ExitLetterType.E, isR2), isLong); 
					}
				}
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 2    
				&& Math.Truncate(ADX(14)[2]) - Math.Truncate(ADX(14)[1]) > 0  
				&& Math.Truncate(ADX(14)[1]) - Math.Truncate(ADX(14)[0]) > 0
				&& DoRoundNoMult(ADX(14)[2]) - DoRoundNoMult(ADX(14)[0]) >= 2
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])    
				&& DoRoundMult(Range()[1]) >= DoRoundMult(ATR(14)[1])
				&& DoRoundMult(Range()[2]) >= DoRoundMult(ATR(14)[2])           
				//&& DoRoundMult(Range()[0]) > DoRoundMult(Range()[2]) + 1 //new//check +- 1
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(EMA(14)[0])) <= 0 //new
				
				&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"0"}) - getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"1"})) < 0
				&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"1"}) - getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"2"})) < 0
				&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, !isLong, new String[]{"0"}) - getUniqueFunctions(UniqueFunctionType.HIGHLOW, !isLong, new String[]{"1"})) < 0
				&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, !isLong, new String[]{"1"}) - getUniqueFunctions(UniqueFunctionType.HIGHLOW, !isLong, new String[]{"2"})) < 0
							
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(Close[0])) > 0
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(Close[1])) > 0
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[2]) - DoRoundMult(Close[2])) > 0
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[0])) < -1
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Open[1])) < -1
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Open[2])) < -1			
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Close[0])) > 2 
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Close[1])) > 2 
			){  
				int LookBackBars = 4;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.F, isR2), isLong);
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 5	
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])
					&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"0"}) - getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"1"}) - 3) < 0
					&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"1"}) - getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"2"}) - 3) < 0
					&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"2"}) - getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"3"}) - 3) < 0
					&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"3"}) - getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"4"}) - 3) < 0
					&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"4"}) - getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"5"}) - 3) < 0
						//&& getDirection(isLong) * (DoRoundMult(Close[5]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","5"})) <= 0
					
					&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new string[]{"5"}) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","5"}) + 1) < 0
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(EMA(35)[0])) < 0 //this is correct
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1])) < -2
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(EMA(3)[0])) <= -1
				&& getDirection(isLong) * (DoRoundMult(Close[5]) - DoRoundMult(EMA(3)[5])) <= 0 
				&& getDirection(isLong) * (DoRoundMult(Close[4]) - DoRoundMult(EMA(3)[4])) < 0
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) >= 0  
				&& getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Close[1])) >= 0  
				&& getDirection(isLong) * (DoRoundMult(Open[2]) - DoRoundMult(Close[2])) >= 0  
				&& getDirection(isLong) * (DoRoundMult(Open[3]) - DoRoundMult(Close[3])) >= 0  
				&& getDirection(isLong) * (DoRoundMult(Open[4]) - DoRoundMult(Close[4])) >= 0  
				&& getDirection(isLong) * (DoRoundMult(Open[5]) - DoRoundMult(Close[5])) >= 0  //quite beneficial
				
				//&& getDirection(isLong) * (DoRoundMult(Close[5]) - DoRoundMult(Open[BarsSinceEntry()])) >= 0
				&& ((getDirection(isLong) * (DoRoundMult(Close[5]) - DoRoundMult(Open[BarsSinceEntry()])) >= 0 
				     && getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry()])) >= 0)
				     || 
					(getDirection(isLong) * (DoRoundMult(Close[5]) - DoRoundMult(Open[BarsSinceEntry()])) < 0 
				     && getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Open[BarsSinceEntry()])) < 0))
				
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Close[5])) <= -5	
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Close[1])) <= 0  
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Close[2])) <= 0  
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Close[3])) <= 0  
				&& getDirection(isLong) * (DoRoundMult(Close[3]) - DoRoundMult(Close[4])) <= 0  
				&& getDirection(isLong) * (DoRoundMult(Close[4]) - DoRoundMult(Close[5])) < 0  //purposely < not <=
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Close[2])) < -2 
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Close[3])) < -2 
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Close[4])) < -2 
				&& getDirection(isLong) * (DoRoundMult(Close[3]) - DoRoundMult(Close[5])) < -2
			){		
				int LookBackBars = 7;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.G, isR2), isLong);
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 2
				&& DoRoundMult(Range()[0]) > DoRoundMult(Range()[1]) + 1
				&& DoRoundMult(Range()[1]) >= DoRoundMult(ATR(14)[1])

					&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Close[1])) < 0
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) > 0
				&& getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Close[1])) >= 0
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1])) < 0 
				&& getDirection(isLong) * (DoRoundMult(EMA(14)[1]) - DoRoundMult(EMA(35)[1])) < -1
				&& getDirection(isLong) * (DoRoundMult(Open[BarsSinceEntry()]) - DoRoundMult(Close[0])) > 3 * (1 + DoRoundMult(Range()[BarsSinceEntry()])) + 2
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry()])) < -1	
			){		
				int LookBackBars = 3;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short)){
					foreach (IOrder entryOrder in entryOrders.Values){
						string EntryName = entryOrder.Name;
						
						if ((EntryName.Contains("V1") && getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry() + 3])) < -1) ||
							((EntryName.Contains("V2") || EntryName.Contains("V3") || EntryName.Contains("V4")) && getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry() + 4])) < -1) ||
							(EntryName.Contains("V5") && getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry() + 5])) < -1))
						getOut(getExitLabel(directionType, phaseType, ExitLetterType.H, isR2), isLong);
					}
				}
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 1
				
				&& (getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry()])) >= -1 && getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Open[BarsSinceEntry()])) >= 0
				||
				getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry()])) < -1 && getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Open[BarsSinceEntry()])) < 0
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(EMA(35)[0])) >= 0)
				
				
				
				&& (getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Bollinger(2,14).Middle[0])) > 1	//test >= 
					&& getDirection(isLong) * (DoRoundMult(Close[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","0"})) < -1 //test <=
					&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])
					||
					getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Bollinger(2,14).Middle[0])) < -1 
					&& getDirection(isLong) * (DoRoundMult(Open[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","0"})) >= 0
					&& DoRoundMult(Range()[0]) >= 3 * DoRoundMult(ATR(14)[0]))
				
					&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, !isLong, new string[]{"0"}) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","0"})) < 0
					&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[0])) < 0	
				//&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(EMA(35)[0])) >= 0	
				&& !(getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(EMA(14)[0])) < -2 && getDirection(isLong) * (DoRoundMult(EMA(14)[0]) - DoRoundMult(EMA(35)[0])) < -2)
			){
				int LookBackBars = 2;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.J, isR2), isLong);
			}
			if (!exitbegun
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0]) 
				&& DoRoundMult(Range()[0]) + 1 >= DoRoundMult(Range()[2])
				
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","0"})) <= 0
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","1"})) <= 0
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","2"})) <= 0

				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[0])) <= 0	
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Open[1])) <= 0	
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Open[2])) <= 0	
				
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) > getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Close[1]))
				&& getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Close[1])) > getDirection(isLong) * (DoRoundMult(Open[2]) - DoRoundMult(Close[2]))
			
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Close[0])) > 0 
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Close[1])) > 0 
				
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(EMA(14)[0])) < 0
				&& getDirection(isLong) * (DoRoundMult(EMA(14)[0]) - DoRoundMult(EMA(35)[0])) < 0
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1])) < 0
				&& getDirection(isLong) * (DoRoundMult(EMA(14)[1]) - DoRoundMult(EMA(35)[1])) <= 0//
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[2]) - DoRoundMult(EMA(14)[2])) < 0
				&& getDirection(isLong) * (DoRoundMult(EMA(14)[2]) - DoRoundMult(EMA(35)[2])) < 1//	//3 35
					&& getDirection(isLong) * (DoRoundMult(EMA(3)[2]) - DoRoundMult(EMA(35)[2])) <= 0//	//3 35
				
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(EMA(14)[0])) < getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1]))
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1])) < getDirection(isLong) * (DoRoundMult(EMA(3)[2]) - DoRoundMult(EMA(14)[2]))
				&& getDirection(isLong) * (DoRoundMult(EMA(14)[0]) - DoRoundMult(EMA(35)[0])) < getDirection(isLong) * (DoRoundMult(EMA(14)[1]) - DoRoundMult(EMA(35)[1]))
				&& getDirection(isLong) * (DoRoundMult(EMA(14)[1]) - DoRoundMult(EMA(35)[1])) < getDirection(isLong) * (DoRoundMult(EMA(14)[2]) - DoRoundMult(EMA(35)[2]))
				
				&& getDirection(isLong) * (DoRoundMult(Open[2]) - DoRoundMult(Open[BarsSinceEntry()])) < 0
			){
				int LookBackBars = 4;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.K, isR2), isLong);
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 11		
				&& DoRoundMult(Range()[0]) >= 2 * (DoRoundMult(ATR(14)[1]) - 1)
				
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Bollinger(2,14).Middle[0])) < -1
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Bollinger(2,14).Middle[1])) < 0
					&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, !isLong, new string[]{"1"}) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","1"}) - 2) > 0 //both lines
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","0"})) > 1
				&& getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Close[1])) >= 0
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry()])) > 0		
				
				&& getDirection(isLong) * 5 * (DoRoundMult(Close[0]) - getUniqueFunctions(UniqueFunctionType.HIGHLOW, !isLong, new string[]{"0"})) >= DoRoundMult(4 * Range()[0])		
			){
				int LookBackBars = 3;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.L, isR2), isLong);
			}						
			if (!exitbegun
				&& BarsSinceEntry() >= 60

				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry()])) > 1
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) > 0			
				&& getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Close[1])) >= 0
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Close[1])) < 0
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(EMA(3)[1])) <= 0
				
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","0"})) <= 0 //or < ?? put r>atr here...
				&& getDirection(isLong) * (DoRoundMult(EMA(14)[1]) - DoRoundMult(EMA(35)[1])) <= -4
				&& getUniqueFunctions(UniqueFunctionType.MINMAX, isLong, new String[]{"60"}) >= 0
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1])) < 0
					&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"60"}) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","60"})) >= 0
			){	
				int LookBackBars = 61;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.M, isR2), isLong);
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 120	
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry()])) < -1
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) > 0
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Close[1])) < 0
				
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","1"})) <= 0 
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","0"})) < 0 
				&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, !isLong, new string[]{"1"}) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","1"})) < 0
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1])) < 0
				&& getUniqueFunctions(UniqueFunctionType.MINMAX, isLong, new String[]{"120"}) >= 0
				&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new String[]{"120"}) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","120"})) >= 0
			){		
				int LookBackBars = 121;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.N, isR2), isLong);
			}
			if (!exitbegun
				&& BarsSinceEntry() > this.lookBackPeriod //+ Convert.ToInt32(entryOrders.Count > 1) * BarsSinceEntry(R2signal)	// cancel if can fit in an avg entry price...
				&& DoRoundNoMult(ADX(14)[0]) - DoRoundNoMult(ADX(14)[1]) <= -2  
					&& DoRoundNoMult(ADX(14)[0]) - DoRoundNoMult(ADX(14)[2]) < -2  
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])
				
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Open[1])) <= 0 
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(EMA(3)[1])) < 0
				&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, !isLong, new string[]{"0"}) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","0"}) - 1) <= 0 
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry()])) > 51 
	//      	&& highesthighindex < 11	//conclusively better w/o
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Close[1])) < -2 
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(EMA(35)[0])) > -1 
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(EMA(14)[0])) < -1 
					//&& getDirection(isLong) * DoRoundMult(EMA(3)[1] - EMA(14)[1]) < 0 
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","0"})) < -2 
			
//				&& getDirection(isLong) * 5 * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry()]))  <=
//				getDirection(isLong) * (DoRoundNoMult(4 * (getUniqueFunctions(UniqueFunctionType.HL, isLong, null) - DoRoundMult(Open[BarsSinceEntry()]) - 1)))	
				
				&& getDirection(isLong) * (5 * DoRoundMult(Close[0]) - 4 * DoRoundNoMult((getUniqueFunctions(UniqueFunctionType.HL, isLong, null) - 1))) <= 0
				
				&& ((inR2? 
					(getDirection(isLong) * ( getUniqueFunctions(UniqueFunctionType.HL, isLong, null) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14", "" + 
					(!isLong? (this.lookBackPeriod - HLCache.getLowestLow().getBarCount(this) -1) : (this.lookBackPeriod - HLCache.getHighestHigh().getBarCount(this) - 1))})) >= -2) :
					(getDirection(isLong) * ( getUniqueFunctions(UniqueFunctionType.HL, isLong, null) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14", "" + 
					(!isLong? (this.lookBackPeriod - HLCache.getLowestLow().getBarCount(this) -1) : (this.lookBackPeriod - HLCache.getHighestHigh().getBarCount(this) - 1))})) >= 0))
					||
					(getDirection(isLong) * ( getUniqueFunctions(UniqueFunctionType.HL, isLong, null) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14", "" + 
					(!isLong? (this.lookBackPeriod - HLCache.getLowestLow().getBarCount(this) -1) : (this.lookBackPeriod - HLCache.getHighestHigh().getBarCount(this) - 1))})) >= -1
					&& getDirection(isLong) * (DoRoundMult(Close[!isLong? (this.lookBackPeriod - HLCache.getLowestLow().getBarCount(this) -1) : (this.lookBackPeriod - HLCache.getHighestHigh().getBarCount(this) - 1)]) - 
					DoRoundMult(Open[!isLong? (this.lookBackPeriod - HLCache.getLowestLow().getBarCount(this) -1) : (this.lookBackPeriod - HLCache.getHighestHigh().getBarCount(this) - 1)])) > 0	
					&& getDirection(isLong) * (DoRoundMult(Open[!isLong? (this.lookBackPeriod - HLCache.getLowestLow().getBarCount(this) -1) : (this.lookBackPeriod - HLCache.getHighestHigh().getBarCount(this) - 1)]) - 	
					DoRoundMult(EMA(3)[!isLong? (this.lookBackPeriod - HLCache.getLowestLow().getBarCount(this) -1) : (this.lookBackPeriod - HLCache.getHighestHigh().getBarCount(this) - 1)])) >= 0))
			){
				int LookBackBars = 36;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.P, isR2), isLong);
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 0
				&& DoRoundMult(ATR(14)[0]) > 1
				&& DoRoundMult(Range()[0]) > 2 * DoRoundMult(Range()[1])
				&& DoRoundMult(Range()[1]) > 1
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) > 0
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) >  2 * (DoRoundMult(ATR(14)[0]) +1 ) 	//not range
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry()])) < -3
					&& getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Close[1])) >= 0
						//&& getDirection(isLong) * (DoRoundMult(Open[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","0"}))  0 //)
					&& getDirection(isLong) * (getUniqueFunctions(UniqueFunctionType.HIGHLOW, isLong, new string[]{"0"}) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","0"})) <= 0
			){	
				int LookBackBars = 3;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.R, isR2), isLong);
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 1
				&& ((getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Bollinger(2,14).Middle[0])) > 0 && getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Open[BarsSinceEntry()])) >= 0)	//2nd pass
					||
					getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Open[BarsSinceEntry()])) < 0)
				
				&& getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Bollinger(2,14).Middle[1])) > 0
				//&& DoRoundMult(ATR(14)[1]) > 1
				&& ((getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) < 0				
					&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Bollinger(2,14).Middle[0])) >= 0 
					&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[0])) >= 6 * DoRoundMult(ATR(14)[1]) - 1 /*&& DoRoundMult(ATR(14)[1]) > 1*/)
					||
					(DoRoundMult(Range()[1]) >= DoRoundMult(ATR(14)[1])
					&& DoRoundMult(Range()[0]) > 4 * DoRoundMult(ATR(14)[0])    //helps gc 9/27 122000
					&& DoRoundMult(Range()[0]) - 2 * DoRoundMult(ATR(14)[0]) > DoRoundMult(Range()[1])
					&& getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Close[1])) <= 0//
					&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) < 0//
					&& getDirection(isLong) * 5 * (DoRoundMult(Close[0]) - getUniqueFunctions(UniqueFunctionType.HIGHLOW, !isLong, new string[]{"0"})) >= DoRoundMult(4 * Range()[0])))	
			){     
				int LookBackBars = 3;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.S, isR2), isLong);
			}  
			if (!exitbegun
				&& BarsSinceEntry() > 20	//accounts for v4b and v5 entries
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])
				&& ToTime(Time[0]) >= _EndTime
				&& ToTime(Time[0].AddMinutes(-20)) < _EndTime 		//smaller possiblie range of 20min, still can tweak//VERY IMPT!!
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","0"})) < 0 
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) > 0 	
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry()])) < 0	
			){
				//int LookBackBars = 0;	//this .T don't need
				if (/*EXIT_TIME_BREAK_CHECKER (LookBackBars) && (*/isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short)//)
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.T, isR2), isLong);
			}
			if (!exitbegun
				&& ((ToTime(Time[0]) >= flattenfd && ToTime(Time[0]) <= flatten2fd && Instrument.FullName.StartsWith("FDAX")) ||
					(ToTime(Time[0]) >= flattenclgcsi && ToTime(Time[0]) <= flatten2clgcsi && (Instrument.FullName.StartsWith("CL") || Instrument.FullName.StartsWith("SI") || Instrument.FullName.StartsWith("GC"))) ||
					(ToTime(Time[0]) >= flatten6e && ToTime(Time[0]) <= flatten26e && Instrument.FullName.StartsWith("6E")))
					|| 
					this.CheckIsHoliday(Time[0].AddMinutes(5))	
			){
				//int LookBackBars = 0;	//this .U don't need...
				if (/*EXIT_TIME_BREAK_CHECKER (LookBackBars) && (*/isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short)//)
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.U, isR2), isLong);
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 10
				&& checkBarVal(10, 5, isLong)
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0]) //less 2
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) > 1
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(EMA(3)[0])) <= 0	
				&& DoRoundMult(Range()[0]) >= 2
			){
				int LookBackBars = 10;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.V, isR2), isLong);
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 3	
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])
				&& DoRoundNoMult(ADX(14)[2]) - DoRoundNoMult(ADX(14)[0]) > 3 
				
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(EMA(14)[0])) < getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1])) -2
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1])) < getDirection(isLong) * (DoRoundMult(EMA(3)[2]) - DoRoundMult(EMA(14)[2])) -2		
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[2]) - DoRoundMult(EMA(14)[2])) < -1 //account for non base10mult
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1])) < -2
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(EMA(14)[0])) < -3
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(EMA(3)[0])) < 0
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(EMA(3)[1])) < 0 
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(EMA(3)[2])) < 0
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) > 1
				&& getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Close[1])) > 1
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Close[1])) > 1
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Close[0])) > 1
				
				&& getUniqueFunctions(UniqueFunctionType.HIGHLOW, !isLong, new string[]{"0"}) < getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","0"}) //***
			){
				int LookBackBars = 4;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.W, isR2), isLong);
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 3				
				&& DoRoundMult(Range()[2]) >= DoRoundMult(ATR(14)[2])
				&& DoRoundNoMult(ADX(14)[2]) - DoRoundNoMult(ADX(14)[0]) >= 3 		
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[2])
					&& getDirection(isLong) * (DoRoundMult(Close[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","0"})) <= 0 //)
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(EMA(14)[0])) < -2
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1])) < 1
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1])) > -1
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[2]) - DoRoundMult(EMA(14)[2])) > 2			
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(EMA(14)[0])) < getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1]))
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1])) < getDirection(isLong) * (DoRoundMult(EMA(3)[2]) - DoRoundMult(EMA(14)[2]))				
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(EMA(3)[0])) < 0
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(EMA(3)[1])) < 0 
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(EMA(3)[2])) < 0 	
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Close[1])) > 1
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Close[0])) > 1
			){
				int LookBackBars = 4;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.X, isR2), isLong);
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 2		
				&& DoRoundNoMult(ADX(14)[1]) - DoRoundNoMult(ADX(14)[0]) > 0
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])
				&& DoRoundMult(ATR(14)[0] - ATR(14)[1]) > 0//DoRoundMult(ATR(14)[1])***
				
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(EMA(14)[0])) < -6
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1])) < -5
				&& getDirection(isLong) * (DoRoundMult(EMA(3)[0]) - DoRoundMult(EMA(14)[0])) < getDirection(isLong) * (DoRoundMult(EMA(3)[1]) - DoRoundMult(EMA(14)[1])) 
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(EMA(3)[0])) < 0
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(EMA(3)[1])) < 0 
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(EMA(3)[2])) < 0 
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) > 1
				&& getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Close[1])) > 1			
				&& getDirection(isLong) * (DoRoundMult(Close[2]) - DoRoundMult(Close[1])) > 1
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Close[0])) > 1
			
				&& getUniqueFunctions(UniqueFunctionType.HIGHLOW, !isLong, new string[]{"0"}) < getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","0"})//***
			){
				int LookBackBars = 4;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.Y, isR2), isLong);
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 2	
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Bollinger(2,14).Middle[0])) <= 0//?!
				&& ((DoRoundMult(Range()[0]) == 0 
				    && DoRoundMult(Range()[1]) == 0 
				    && DoRoundMult(Range()[2]) == 0 
				    && DoRoundMult(Close[2]) - DoRoundMult(Close[0]) == 0)	//else it could have different closes...
			  		||
					(DoRoundMult(Close[0]) - DoRoundMult(Close[1]) == 0 
				    && DoRoundMult(Close[1]) - DoRoundMult(Close[2]) == 0 
				    && DoRoundMult(Close[2]) - DoRoundMult(Close[3]) == 0
					&& DoRoundMult(Range()[0]) < 1 
				    && DoRoundMult(Range()[1]) <= 1 
				    && DoRoundMult(Range()[2]) <= 1 
				    && DoRoundMult(Range()[3]) <= 1
					
					&& getDirection(isLong) * (DoRoundMult(Open[0]) - DoRoundMult(Close[0])) == 0
					&& getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Close[1])) == 0
					&& getDirection(isLong) * (DoRoundMult(Open[2]) - DoRoundMult(Close[2])) == 0
					&& getDirection(isLong) * (DoRoundMult(Open[3]) - DoRoundMult(Close[3])) == 0
					&& DoRoundMult(Range()[4]) <= 1))
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry()])) <= 0		
			){
				int LookBackBars = 5;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.ZZ, isR2), isLong);
			}
			if (!exitbegun
				&& BarsSinceEntry() >= 2	
				&& getDirection(isLong) * (DoRoundMult(Close[1]) - DoRoundMult(Bollinger(2,14).Middle[1])) > 0
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])
				&& getDirection(isLong) * (DoRoundMult(Open[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, isLong, new String[]{"2","14","0"})) <= 0
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - getUniqueFunctions(UniqueFunctionType.BOLLINGER, !isLong, new String[]{"2","14","0"})) >= 0
				
				&& getDirection(isLong) * (DoRoundMult(Open[1]) - DoRoundMult(Close[1])) <= 0
				
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[BarsSinceEntry()])) > 0			
			){
				int LookBackBars = 3;
				if (EXIT_TIME_BREAK_CHECKER (LookBackBars) && (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short))
					getOut(getExitLabel(directionType, phaseType, ExitLetterType.ZZZ, isR2), isLong);
			}
		}

		public double getUniqueFunctions(UniqueFunctionType uniqueFunctionType, bool isLong, string[] inputParams){
			switch (uniqueFunctionType){
				case (UniqueFunctionType.BOLLINGER):{
					if (isLong)
						return DoRoundMult(Bollinger(int.Parse(inputParams[0]),int.Parse(inputParams[1])).Lower[int.Parse(inputParams[2])]);
					else
						return DoRoundMult(Bollinger(int.Parse(inputParams[0]),int.Parse(inputParams[1])).Upper[int.Parse(inputParams[2])]);
				break;
				}
				case (UniqueFunctionType.DI):{	
					if (isLong)
						return DoRoundNoMult(DM(14).DiPlus[int.Parse(inputParams[0])]);
					else
						return DoRoundNoMult(DM(14).DiMinus[int.Parse(inputParams[0])]);
				break;
				}
				case (UniqueFunctionType.MINMAX):{
					if (isLong) 
						return DoRoundMult(High[int.Parse(inputParams[0])]) - DoRoundMult(MAX(High, int.Parse(inputParams[0]))[0]);
					else
						return DoRoundMult(MIN(Low, int.Parse(inputParams[0]))[0]) - DoRoundMult(Low[int.Parse(inputParams[0])]);
				break;
				}
				case (UniqueFunctionType.HIGHLOW):{
					if (isLong) 
						return DoRoundMult(High[int.Parse(inputParams[0])]);
					else
						return DoRoundMult(Low[int.Parse(inputParams[0])]);
				break;
				}		
				case (UniqueFunctionType.HL):{
					if (isLong) 
						return highestHigh ;
					else
						return lowestLow ;
				break;
				}
				case(UniqueFunctionType.TWO14):{	
					double returnIndex = 0;
					int lookback = 2;
					int lookbackVolume = Math.Min(20, BarsSinceEntry()) - lookback;
					
					if ((DoRoundMult(MAX(High, lookbackVolume)[lookback]) - 1 <= DoRoundMult(MAX(High, 2)[0])) && isLong || (DoRoundMult(MIN(Low, lookbackVolume)[lookback]) + 1 >= DoRoundMult(MIN(Low, 2)[0]) && !isLong))
						returnIndex = 1;
					return returnIndex;
				break;
				}
			}
			return 0;
		}
		
		private bool checkBarVal( int lookBackPeriod, int threshholdCount, bool isLong ){
			int fiveCount = 0;
			double compareValue = (isLong) ? this.DoRoundMult(this.High[0]) : this.DoRoundMult(this.Low[0]);
			double compareValue2 = 0;
			
			for (int i = 0; i < lookBackPeriod; i++){
				if (((isLong) ? this.DoRoundMult(this.High[i]) : this.DoRoundMult(this.Low[i])) == compareValue	
					&& this.DoRoundMult(Range()[i]) > 0 
					&& ((isLong) ? (this.DoRoundMult(this.Open[i]) >= this.DoRoundMult(this.Close[i])) : (this.DoRoundMult(this.Open[i]) <= this.DoRoundMult(this.Close[i])))){	//every bar
						
					fiveCount++;
					if (fiveCount >= threshholdCount){
						compareValue2 = (isLong) ? this.DoRoundMult(this.Open[i]) : this.DoRoundMult(this.Close[i]);
					
						if (this.DoRoundMult(Range()[i]) >= this.DoRoundMult(ATR(14)[i]) && ((isLong) ? this.DoRoundMult(this.Close[i]) : this.DoRoundMult(this.Open[i])) < compareValue2
							&& this.DoRoundMult(Range()[i]) >= 2)	//the 5th one?!
							return true;	
					}
				}
			}
			return false;
		}
		
		public bool EXIT_TIME_BREAK_CHECKER (int doEXIT_RULE_AA_PARAM_LASTBAR){
			bool result = true;
		
			result &= ToTime(Time[doEXIT_RULE_AA_PARAM_LASTBAR]) == ToTime(Time[0].AddMinutes(-doEXIT_RULE_AA_PARAM_LASTBAR));
			//result &= ToTime(Time[0]) == ToTime(Time[0].AddMinutes(-doEXIT_RULE_AA_PARAM_LASTBAR));
			return result;
		}
		
		protected bool zStop(int cellone, int celltwo, int version, bool isLong, bool isR2){ 
			DirectionType directionType = isLong ? DirectionType.L:DirectionType.S ;
			PhaseType phaseType = PhaseType.X ;
			if (BarsSinceEntry() >= 1	
				&& Math.Truncate(ADX(14)[celltwo]) - Math.Truncate(ADX(14)[cellone]) > 0	
				&& Math.Round(ADX(14)[cellone]) - Math.Round(ADX(14)[0]) > 0
				&& Math.Truncate(ADX(14)[celltwo]) - Math.Truncate(ADX(14)[0]) >= Convert.ToInt32(celltwo) -1
			
				&& DoRoundMult(Range()[0]) >= DoRoundMult(ATR(14)[0])
				//&& DoRoundMult(Range()[celltwo]) >= DoRoundMult(ATR(14)[celltwo])
				&& DoRoundMult(Range()[0]) >= DoRoundMult(Range()[cellone])
				
				&& getDirection(isLong) * (DoRoundMult(Close[cellone]) - DoRoundMult(Close[celltwo])) < - 1 //ok
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Close[cellone])) <= - 1
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Close[celltwo])) < -(1 + Convert.ToInt32(celltwo))		
				&& getDirection(isLong) * (DoRoundMult(Close[celltwo]) - DoRoundMult(Open[celltwo])) < 0	
				&& getDirection(isLong) * (DoRoundMult(Close[cellone]) - DoRoundMult(Open[cellone])) <= 0
				&& getDirection(isLong) * (DoRoundMult(Close[0]) - DoRoundMult(Open[0])) < 0
				
				&& getDirection(isLong) * (DoRoundMult(Close[celltwo]) - DoRoundMult(EMA(3)[celltwo])) < -2 //!!
				&& getDirection(isLong) * (DoRoundMult(Close[cellone]) - DoRoundMult(EMA(3)[cellone])) < -1 
			){
				if (isLong && Position.MarketPosition == MarketPosition.Long || !isLong && Position.MarketPosition == MarketPosition.Short){ //no EXIT_TIME_BREAK_CHECKER (LookBackBars) because cant int it...
					if (!inR2 && getDirection(isLong) * (DoRoundMult(Close[celltwo]) - DoRoundMult(Open[BarsSinceEntry()])) < -1){						
						getOut(getExitLabel(directionType, phaseType, ExitLetterType.Z, inR2), isLong);
						return true;
					}
					
					else if (inR2 && getDirection(isLong) * (DoRoundMult(Close[celltwo]) - DoRoundMult(Open[BarsSinceEntry(R2signal)])) < -1){
						getOut(getExitLabel(directionType, phaseType, ExitLetterType.Z, inR2), isLong);
						inR2 = false; 
						return true;	
					}	
				}
			}
			return false;
		}

		public string getExitLabel(DirectionType directionType, PhaseType phaseType, ExitLetterType exitLetterType, bool isR2){ 
			string isR2String = isR2 ? "_R2": "";
			return (directionType + "" + phaseType + exitLetterType + isR2String);
		}
		
		long orderIndex = 0;
		protected string getOrderID(DirectionType directionType, PhaseType phaseType, VType vType, VolSizeType volSizeType, DateTime time, bool isR2){
			return getOrderID(directionType, phaseType, vType, volSizeType, time.ToString("yy-MM-dd HH:mm:ss"), isR2);
		}
		
		protected string getOrderID(DirectionType directionType, PhaseType phaseType, VType vType, VolSizeType volSizeType, string time, bool isR2){
			RType rType = RType.R1;
			if (isR2)
				rType = RType.R2;	
			return Instrument.MasterInstrument.Name + "_" + directionType.ToString() + phaseType.ToString() + "_" + vType.ToString() + "_" + volSizeType.ToString() + "_" + time + "_" + rType.ToString();
		}

		protected string generateOrderID(DirectionType directionType, VType vType, VolSizeType volSizeType, bool isR2){
			RType rType = RType.R1;
			if (isR2)
				rType = RType.R2;
			return Instrument.MasterInstrument.Name + "_" + directionType.ToString() + "_" + vType.ToString() + "_" + volSizeType.ToString() + "_" + Time[0].AddMinutes(-1).ToString("yy-MM-dd HH:mm:ss") + "_" + rType.ToString();
		}

		protected bool daylightsavings(){
	//  globalcounter ++;
		TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("US Eastern Standard Time");
		TimeZoneInfo timeZoneInfoEuro = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");	//for countries whose DST changes on diff dates)
		DateTime currentDate = Time[0];
			if (Instrument.FullName.Contains("FDAX"))
				return timeZoneInfoEuro.IsDaylightSavingTime(currentDate);
			else
				return timeZoneInfo.IsDaylightSavingTime(currentDate);
		}
		
		protected bool CheckIsHoliday(DateTime currentTime){
			return HolidayWidget3.getInstance().CheckIsHoliday(currentTime, Instrument.MasterInstrument.Name); //.Substring(0,2) );
		} 
	}
}