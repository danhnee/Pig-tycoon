import type { WeatherType } from './enums.ts';
import { type PigData } from './pig.ts';
import { type FarmState } from './farm.ts';
import { type HerdState } from './herd.ts';
import { type DayMarketState, type NightMarketListing, type MysticMerchantState } from './market.ts';
import { type BuildingHallState, type DefenseBuildingInstance } from './defense.ts';
import { type CharacterData } from './character.ts';

export interface ClockSaveState {
  currentDay: number;
  currentHour: number;
  currentMinute: number;
  cycleNumber: number;
  cycleTotalDays: number;
  cycleDayIndex: number;
  daysUntilMajorWave: number;
  minorWavesRemainingInCycle: number;
  hoursUntilNextMinorWave: number;
  sleepCooldownHoursRemaining: number;
}

export interface WeatherSaveState {
  currentWeather: WeatherType;
  durationDaysRemaining: number;
  nextWeatherForecast: WeatherType;
}

export interface GameSaveState {
  version: string; // "6.0.0"
  savedAtTimestamp: number;
  saveName: string;
  
  clock: ClockSaveState;
  weather: WeatherSaveState;
  farm: FarmState;
  pigs: PigData[];
  herd: HerdState;
  
  market: {
    dayMarket: DayMarketState;
    nightListings: NightMarketListing[];
    mysticMerchant: MysticMerchantState;
  };
  
  defense: {
    hall: BuildingHallState;
    buildings: DefenseBuildingInstance[];
  };
  
  character: CharacterData;
  
  statistics: {
    majorWavesSurvived: number;
    minorWavesDefeated: number;
    bossesDefeated: number;
    totalPigsSold: number;
    totalGoldEarned: number;
    discoveredGeneLines: string[];
  };
}
