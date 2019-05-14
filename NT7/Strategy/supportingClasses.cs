#region Using declarations
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Indicator;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Strategy;
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// Enter the description of your strategy here
    /// </summary>
    [Description("Enter the description of your strategy here")]
    public class supportingClasses : Strategy{}
	
	public class VTimings{
		public DateTime start; public DateTime end;	public DateTime cutOff;	public VType vType; 
		public VTimings(VType vType, DateTime start, DateTime end, DateTime cutOff){
			this.vType = vType; this.start = start;	this.end = end;	this.cutOff = cutOff;
		}
	}
	public class VolSizeVar{
		public static int defaultLookBackInterval = 14;
		public static double multiple = 25;
		public static double quad = 50;
		public static int betSizing = 2;
		public static double duo = 25;	
	
		public Dictionary<string, string> R1Slabels = new Dictionary<string, string>();	// For r1 Shorts
		public Dictionary<string, string> R2Slabels = new Dictionary<string, string>();	// For r2 Shorts
		public Dictionary<string, string> R1Llabels = new Dictionary<string, string>();	// For r1 Longs
		public Dictionary<string, string> R2Llabels = new Dictionary<string, string>();	// For r2 Longs	
	}	
	public enum VType{
		V0, V1, V2, V3, V4, V5
	}
	public enum VolSizeType{
		Q = 1, H = 2, F = 4
	}	
	public enum DirectionType{
		L, S //LE, SE	
	}
	public enum PhaseType{
		E, X
	}
	public enum ExitLetterType{
		A, B, C, D, E, F, G, H, J, K, L, M, N, P, Q, R, S, T, U, V, W, X, Y, Z,ZZ,ZZZ,ZZZZ
	}	
	public enum RType{
		R1, R2
	}
	public enum UniqueFunctionType{
		BOLLINGER, DI, MINMAX, MINMAX1, HIGHLOW, HL, TWO14, TWO15
	}

	public class HLDO{	// This following is to enable tracking of Highest Highs and lows of each trade
		PointDO highestHigh = null;
		PointDO lowestLow = null;
		
		public HLDO(int lookBackPeriod){
			this.highestHigh = new HighPointDO(lookBackPeriod);
			this.lowestLow = new LowPointDO(lookBackPeriod);
		}
		
		public PointDO getHighestHigh(){
			return this.highestHigh;
		}
		
		public PointDO getLowestLow(){
			return this.lowestLow;
		}
	}
	
	public class HighPointDO : PointDO {
		public HighPointDO( int lookBackPeriod ): base( lookBackPeriod ){} // Constructor
		
		public override void checkNewHL( Strategy strategy ){
			if (!isActive)
				return;
				
			if ( cache.Count >= lookBackPeriod ){ // Remove / Pop the last value of the cache 
				if ( this.barCount == 0 ){ // Check if the current Bar Count is the current Highest/Lowest
					barCount = cache.Count - 1;
					
					for ( int i = 1 ; i <  cache.Count ; i++ ) // Search for the next LowestLow / Highest High
						if ( cache[ cache.Count - i] > cache[barCount] )
							barCount = cache.Count - i;	
				}
				
				cache.RemoveAt(0); // Remove the last value
				barCount--; // Increment the positioning of the barCount
			}
			cache.Add( strategy.High[0] ); // Add the new Bar value into the cache
			
			if ( cache[ cache.Count -1 ] >= cache[this.barCount] ) // Check if the current bar is the lowest low/ higest high
				this.barCount = cache.Count -1 ;
		}
	}
	
	public class LowPointDO : PointDO {
		public LowPointDO( int lookBackPeriod ): base( lookBackPeriod ){} // Constructor
		
		public override void checkNewHL( Strategy strategy ){
			if (!isActive)
				return;
			
			if ( cache.Count >= lookBackPeriod ){ // Remove / Pop the last value of the cache 
				if ( this.barCount == 0 ){ // Check if the current Bar Count is the current Highest/Lowest
					barCount = cache.Count - 1;
					
					for ( int i = 1 ; i <  cache.Count ; i++ ) // Search for the next LowestLow / Highest High
						if ( cache[ cache.Count - i] < cache[barCount] )
							barCount = cache.Count - i;	
				}
				
				cache.RemoveAt(0); // Remove the last value
				barCount--; // Increment the positioning of the barCount
			}
			cache.Add( strategy.Low[0] ); // Add the new Bar value into the cache
			
			if ( cache[ cache.Count -1 ] <= cache[this.barCount] ) // Check if the current bar is the lowest low/ higest high
				this.barCount = cache.Count -1 ;
		}
	}
	
	public abstract class PointDO {
		protected int lookBackPeriod = 0; // Set a List to Store Arrays
		protected List<double> cache = new List<double>(); // Set a List to Store Arrays
		protected int barCount = 0; // refers to the number of bars since the begining of a trade	
		protected bool isActive = false; 
		
		public PointDO( int lookBackPeriod ){
			this.lookBackPeriod = lookBackPeriod;
		}
		
		public int getBarCount(Strategy strategy){
			return this.barCount; 
		}
		
		public double getValue(){
			return this.cache[ this.barCount ];
		}
				
		public abstract void checkNewHL( Strategy strategy );

		public void start(){
			kill();
			isActive = true;
		}
		
		public void kill(){
			isActive = false;
			cache.Clear();
			barCount= 0;
		}
		
		public bool getIsActive(){
			return this.isActive;
		}
	}
}
