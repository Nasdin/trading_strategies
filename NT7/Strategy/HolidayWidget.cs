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
namespace NinjaTrader.Strategy{
    public class HolidayWidget : Strategy{
		TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("US Eastern Standard Time");
		TimeZoneInfo timeZoneInfoLocal = TimeZoneInfo.FindSystemTimeZoneById("US Eastern Standard Time");

        protected override void Initialize(){   
			List<string> MarketIDS = new List<string>();
			MarketIDS.Add("GC"); MarketIDS.Add("SI"); MarketIDS.Add("CL"); MarketIDS.Add("6E");
			
			int USAHours = 0;	// Your destination market
			int LOCALHours = 0; // Your local market

			foreach (string marketId in MarketIDS){		
				if (marketId != "6E"){
					Holiday holiday1 = new Holiday("FXME", HolidayDateType.EXACT_DAY);	 //FXME = fully defined Christmas Eve; Christmas Day is whole day closed; NYD whole day closed, NYE regular close
					loadMarket(marketId, holiday1);
					holiday1.setEXACT_DAY_Date( new DateTime(9999, 12, 24, 0, 0, 0));
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday1.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday1.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday1.setStartHour(13.75f  + (LOCALHours - USAHours)); //will exit next available minute if that min got no volume...
					holiday1.setHolidayDuration(4.25f); 

					Holiday holiday2 = new Holiday("PBFR", HolidayDateType.PART_DEF);    //PBFR = Partially defined Black Friday
					loadMarket(marketId, holiday2);
					holiday2.setPART_DEF_Date(HolidayDay.FRI, HolidayWeek.FOURTH, HolidayMonth.NOV);   
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday2.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday2.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday2.setStartHour(13.75f + (LOCALHours - USAHours));
					holiday2.setHolidayDuration(4.25f);

					Holiday holiday3 = new Holiday("PTHG", HolidayDateType.PART_DEF);    //PTHG = Partially defined ThanksGiving
					loadMarket(marketId, holiday3);
					holiday3.setPART_DEF_Date(HolidayDay.THU, HolidayWeek.FOURTH, HolidayMonth.NOV);  
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday3.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday3.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					//holiday3.setStartHour(13.25f + (LOCALHours - USAHours));
					//holiday3.setHolidayDuration(4.75f);
					holiday3.setStartHour(13f + (LOCALHours - USAHours));
					holiday3.setHolidayDuration(5f);
	
					// For Part Def Order of lines are important. "loadMarket" must come before "CheckIsHoliday"
					Holiday holiday4 = new Holiday("PLBD", HolidayDateType.PART_DEF);    //PLBD = Partially defined Labor Day
					loadMarket(marketId, holiday4);
					holiday4.setPART_DEF_Date(HolidayDay.MON, HolidayWeek.FIRST, HolidayMonth.SEP);   
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday4.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday4.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					//holiday4.setStartHour(13.25f + (LOCALHours - USAHours));
					holiday4.setStartHour(13f + (LOCALHours - USAHours));
					//holiday4.setHolidayDuration(4.75f);	
					holiday4.setHolidayDuration(5f);	

					Holiday holiday5 = new Holiday("FIND", HolidayDateType.EXACT_DAY);	//FIND = Fully defined Independence day
					loadMarket(marketId, holiday5);
					holiday5.setEXACT_DAY_Date( new DateTime(9999, 7, 4, 0, 0, 0));
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday5.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday5.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday5.setStartHour(13.25f  + (LOCALHours - USAHours)); //will exit next available minute if that min got no volume...
					holiday5.setHolidayDuration(4.75f); 
					

					Holiday holiday6 = new Holiday("PMEM", HolidayDateType.PART_DEF);    //PMEM = Partially defined Memorial Day; good friday is immaterial since whole day close
					loadMarket(marketId, holiday6);
					holiday6.setPART_DEF_Date(HolidayDay.MON, HolidayWeek.LAST, HolidayMonth.MAY);    
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday6.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday6.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday6.setStartHour(13.25f + (LOCALHours - USAHours));
					holiday6.setHolidayDuration(4.75f);	

					
					Holiday holiday7 = new Holiday("PPRE", HolidayDateType.PART_DEF);    //PPRE = Partially defined President's Day
					loadMarket(marketId, holiday7);
					holiday7.setPART_DEF_Date(HolidayDay.MON, HolidayWeek.THIRD, HolidayMonth.FEB);    
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday7.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday7.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday7.setStartHour(13.25f + (LOCALHours - USAHours));
					holiday7.setHolidayDuration(4.75f);

					
					Holiday holiday8 = new Holiday("PMLK", HolidayDateType.PART_DEF);    //PMLK = Partially defined Martin Luther King
					loadMarket(marketId, holiday8);
					holiday8.setPART_DEF_Date(HolidayDay.MON, HolidayWeek.THIRD, HolidayMonth.JAN);   
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday8.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday8.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
					   		  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday8.setStartHour(13.25f + (LOCALHours - USAHours));
					holiday8.setHolidayDuration(4.75f);		
				}
								
				if (marketId == "6E"){
					
					Holiday holiday1_A = new Holiday("FXME", HolidayDateType.EXACT_DAY);	//FXME = fully defined Christmas Eve; Christmas Day is whole day closed; NYD whole day closed, NYE regular close
					loadMarket(marketId, holiday1_A);
					holiday1_A.setEXACT_DAY_Date( new DateTime(9999, 12, 24, 0, 0, 0));
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday1_A.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday1_A.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
						   	  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday1_A.setStartHour(13.25f  + (LOCALHours - USAHours));
					holiday1_A.setHolidayDuration(4.75f);

					Holiday holiday2_A = new Holiday("PBFR_A", HolidayDateType.PART_DEF);    //PBFR = Partially defined Black Friday
					loadMarket(marketId, holiday2_A);
					holiday2_A.setPART_DEF_Date(HolidayDay.FRI, HolidayWeek.FOURTH, HolidayMonth.NOV);   
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday2_A.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday2_A.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday2_A.setStartHour(13.25f + (LOCALHours - USAHours));
					holiday2_A.setHolidayDuration(4.75f);
			
					Holiday holiday3_A = new Holiday("PTHG_A", HolidayDateType.PART_DEF);    //PTHG = Partially defined ThanksGiving
					loadMarket(marketId, holiday3_A);
					holiday3_A.setPART_DEF_Date(HolidayDay.THU, HolidayWeek.FOURTH, HolidayMonth.NOV);   
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday3_A.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday3_A.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday3_A.setStartHour(13f + (LOCALHours - USAHours));
					holiday3_A.setHolidayDuration(5f);

					Holiday holiday4_A = new Holiday("PLBD_A", HolidayDateType.PART_DEF);    //PLBD = Partially defined Labor Day
					loadMarket(marketId, holiday4_A);
					holiday4_A.setPART_DEF_Date(HolidayDay.MON, HolidayWeek.FIRST, HolidayMonth.SEP);   
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday4_A.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday4_A.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday4_A.setStartHour(13f + (LOCALHours - USAHours));
					holiday4_A.setHolidayDuration(5f);					

					Holiday holiday5_A = new Holiday("FIND_A", HolidayDateType.EXACT_DAY);	//FIND = Fully defined Independence day
					loadMarket(marketId, holiday5_A);
					holiday5_A.setEXACT_DAY_Date( new DateTime(9999, 7, 4, 0, 0, 0));
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday5_A.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday5_A.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday5_A.setStartHour(13f  + (LOCALHours - USAHours)); //will exit next available minute if that min got no volume...
					holiday5_A.setHolidayDuration(5f); 

					Holiday holiday6_A = new Holiday("PMEM_A", HolidayDateType.PART_DEF);     //PMEM = Partially defined Memorial Day; good friday is immaterial since whole day close
					loadMarket(marketId, holiday6_A);
					holiday6_A.setPART_DEF_Date(HolidayDay.MON, HolidayWeek.LAST, HolidayMonth.MAY);   
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday6_A.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday6_A.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday6_A.setStartHour(13f + (LOCALHours - USAHours));
					holiday6_A.setHolidayDuration(5f);						

					Holiday holiday7_A = new Holiday("PPRE_A", HolidayDateType.PART_DEF);   //PPRE = Partially defined President's Day
					loadMarket(marketId, holiday7_A);
					holiday7_A.setPART_DEF_Date(HolidayDay.MON, HolidayWeek.THIRD, HolidayMonth.FEB);    
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday7_A.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday7_A.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday7_A.setStartHour(13f + (LOCALHours - USAHours));
					holiday7_A.setHolidayDuration(5f);									
					
					Holiday holiday8_A = new Holiday("PMLK_A", HolidayDateType.PART_DEF);    //PMLK = Partially defined Martin Luther King
					loadMarket(marketId, holiday8_A);
					holiday8_A.setPART_DEF_Date(HolidayDay.MON, HolidayWeek.THIRD, HolidayMonth.JAN);   
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday8_A.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday8_A.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday8_A.setStartHour(13f + (LOCALHours - USAHours));
					holiday8_A.setHolidayDuration(5f);					
						
					//
					Holiday holiday4_B = new Holiday("PLBD_B", HolidayDateType.PART_DEF);    //PLBD = Partially defined Labor Day,   && 45min early close prior fri for 6E
					loadMarket(marketId, holiday4_B);
					holiday4_B.setPART_DEF_Date(HolidayDay.MON, HolidayWeek.FIRST, HolidayMonth.SEP);    //45min early close prior day for 6E
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday4_B.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday4_B.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday4_B.setStartHour(-56f + (LOCALHours - USAHours));
					holiday4_B.setHolidayDuration(.75f);

					Holiday holiday6_B = new Holiday("PMEM_B", HolidayDateType.PART_DEF);    //PMEM = Partially defined Memorial Day; good friday is immaterial since whole day close && 45min early close prior fri for 6E
					loadMarket(marketId, holiday6_B);
					holiday6_B.setPART_DEF_Date(HolidayDay.MON, HolidayWeek.LAST, HolidayMonth.MAY);   
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday6_B.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday6_B.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday6_B.setStartHour(-56f + (LOCALHours - USAHours));
					holiday6_B.setHolidayDuration(.75f);
					
					Holiday holiday7_B = new Holiday("PPRE_B", HolidayDateType.PART_DEF);   //PPRE = Partially defined President's Day && 45min early close prior fri for 6E
					loadMarket(marketId, holiday7_B);
					holiday7_B.setPART_DEF_Date(HolidayDay.MON, HolidayWeek.THIRD, HolidayMonth.FEB);   
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday7_B.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday7_B.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday7_B.setStartHour(-56f + (LOCALHours - USAHours));
					holiday7_B.setHolidayDuration(.75f);					
					
					Holiday holiday8_B = new Holiday("PMLK_B", HolidayDateType.PART_DEF);    //PMLK = Partially defined Martin Luther King && 45min early close prior fri for 6E
					loadMarket(marketId, holiday8_B);
					holiday8_B.setPART_DEF_Date(HolidayDay.MON, HolidayWeek.THIRD, HolidayMonth.JAN);   
					HolidayWidget3.getInstance().CheckIsHoliday(DateTime.Now, marketId);
					USAHours = timeZoneInfo.IsDaylightSavingTime(holiday8_B.getHolidayDate()) ? timeZoneInfo.BaseUtcOffset.Hours + 1 : timeZoneInfo.BaseUtcOffset.Hours;
					LOCALHours = TimeZoneInfo.Local.IsDaylightSavingTime(holiday8_B.getHolidayDate().AddHours(TimeZoneInfo.Local.BaseUtcOffset.Hours - USAHours))  
							  ? TimeZoneInfo.Local.BaseUtcOffset.Hours + 1 : TimeZoneInfo.Local.BaseUtcOffset.Hours;
					holiday8_B.setStartHour(-56f + (LOCALHours - USAHours));
					holiday8_B.setHolidayDuration(.75f);	
					
				}	
            }
        }
		
		private void loadMarket(string marketId, Holiday holiday){
			HolidayWidget2 HW = null;
			if (!HolidayWidget3.MktHolidays.ContainsKey(marketId)){
				HW = new HolidayWidget2();
				HolidayWidget3.MktHolidays.Add(marketId, HW);
			}
			else
				HW = HolidayWidget3.MktHolidays[marketId];
				HW.InsertHoliday(holiday);
		}
    }

    public class HolidayWidget3{
		private static HolidayWidget3 instance = new HolidayWidget3();
		private HolidayWidget3(){}
		
		public static IDictionary<string, HolidayWidget2> MktHolidays = new Dictionary<string, HolidayWidget2>();  
				
		public static HolidayWidget3 getInstance(){
			if (instance == null)
				instance = new HolidayWidget3();	
			return instance;
		}
		
		public bool CheckIsHoliday(DateTime currentTime, string marketID){
			if (MktHolidays.ContainsKey(marketID))
				return MktHolidays[marketID].CheckIsHoliday (currentTime);
			return false;
		}
	}
	
    public class HolidayWidget2{
		private IDictionary<string, Holiday> Holidays = new Dictionary<string, Holiday>();

		public HolidayWidget2(){
		}

		public bool CheckIsHoliday(DateTime currentTime){
			foreach (Holiday holiday in Holidays.Values){
				if (holiday.getHolidayDate().Year != currentTime.Year){
					Strategy s = new Strategy();	// For Exact Date
					holiday.setEXACT_DAY_Date(holiday.getHolidayDate().AddYears((currentTime.Year - holiday.getHolidayDate().Year)).AddHours(-holiday.getStartHour()));
					holiday.setPART_DEF_Date(currentTime.Year);	// For Partial Date
				}
				if ((holiday.getHolidayDate() <= currentTime) && (currentTime <= holiday.getHolidayDate().AddHours(holiday.getHolidayDuration())))
					return true;
			}
			return false;
		}

		public void InsertHoliday(Holiday holiday){
			if (!Holidays.ContainsKey(holiday.getHolidayName()))
				Holidays.Add(holiday.getHolidayName(), holiday);
		}

		public Holiday getHoliday(string holidayName){
			if (Holidays.ContainsKey(holidayName))
				return Holidays[holidayName];
			return null;
		}
	}

	public class Holiday{
		private HolidayDateType holidayDateType = HolidayDateType.EXACT_DAY;
		private string holidayName = "";
		private DateTime exactDate; // For Exact Dates
		private DateTime partDate; // For Part Dates
		private HolidayMonth month = HolidayMonth.JAN;
		private HolidayWeek week = HolidayWeek.FIRST;
		private HolidayDay day = HolidayDay.MON;
		private float StartHour = 0f; // The following are in hours
		private float HolidayDuration = 24f;
		private Strategy strategy = new Strategy(); // Strategy for printing stuff;

		public Holiday(string holidayName, HolidayDateType holidayDateType){
			this.holidayName = holidayName;
			this.holidayDateType = holidayDateType;
		}

		public void setStartHour(float StartHour){
			this.StartHour = StartHour;
		}

		public void setHolidayDuration(float HolidayDuration){
			this.HolidayDuration = HolidayDuration;
		}

		public void setEXACT_DAY_Date(DateTime exactDate){
			this.exactDate = exactDate;
		}

		public float getStartHour(){
			return this.StartHour;
		}

		public float getHolidayDuration(){
			return this.HolidayDuration;
		}

		public void setPART_DEF_Date(HolidayDay day, HolidayWeek week, HolidayMonth month){
			this.day = day;	this.week = week; this.month = month;
			
			HolidayWidget2 H2 = new HolidayWidget2();
			H2.CheckIsHoliday(DateTime.Now);
		}

		public void setPART_DEF_Date(int year){
			partDate = new DateTime(year, (int)this.month, 1, 0, 0, 0, 0);
			int TotalDays = (int)((TimeSpan)(partDate.AddMonths(1) - partDate)).TotalDays; // This gives us the total days ofthe month we're interested in 
			int correctDay = 1;
			int weekCount = 0; // Get the First Day of the Month
//
//			int counter = 0;
//			
//			for (int i = 0; i < TotalDays; i++){
//				if (partDate.AddDays(i).DayOfWeek.ToString().ToUpper().Contains(day.ToString())){
//					weekCount++;
//					counter = 0;
//				}
//				
//				counter++;
//				
//				if (weekCount == (int)week){
//					correctDay = i + 1;
//					break;
//				}					
//			}
//			
//			if (week == HolidayWeek.LAST)
//				correctDay = ( (weekCount - 1) * 7) + counter + 1;

			for (int i = 0; i < TotalDays; i++)
			{
				if (partDate.AddDays(i).DayOfWeek.ToString().ToUpper().Contains(day.ToString()))
					weekCount++;

				if (weekCount == (int)week)
				{
					correctDay = i + 1;
					break;
				}

				if (week == HolidayWeek.LAST)
				{
					correctDay = i + 1;
					break;
				}
			}
			
			partDate = new DateTime(year, (int)this.month, correctDay, 0, 0, 0, 0);	
		}

		public DateTime getHolidayDate(){
			switch (this.holidayDateType){
				case HolidayDateType.EXACT_DAY:
					return this.exactDate.AddHours(this.StartHour);
				break;

				case HolidayDateType.PART_DEF:
					return this.partDate.AddHours(this.StartHour);
				break;
			}
			return DateTime.Now;
		}

		public string getHolidayName(){
			return this.holidayName;
		}

		private void Print(string message){
			strategy.Print(message);
		}
	}
	
    public enum HolidayDateType{
        EXACT_DAY, PART_DEF 
    }

    public enum HolidayMonth{
        JAN = 1, FEB = 2, MAR = 3, APR = 4, MAY = 5, JUN = 6, JUL = 7, AUG = 8, SEP = 9, OCT = 10, NOV = 11, DEC = 12
    }

    public enum HolidayDay{
        MON, TUE, WED, THU, FRI, SAT, SUN
    }    

    public enum HolidayWeek{
        FIRST = 1, SECOND = 2, THIRD = 3, FOURTH = 4, LAST = 9999
    }	
} 