import { type GameSaveState } from '../types/save.ts';
import * as fs from 'fs';
import * as path from 'path';

export class SaveLoadManager {
  public static serialize(state: type GameSaveState): string {
    return JSON.stringify(state, null, 2);
  }

  public static deserialize(jsonString: string): type GameSaveState {
    const parsed = JSON.parse(jsonString);
    if (!parsed.version || !parsed.clock || !parsed.farm) {
      throw new Error('Dữ liệu lưu trữ không hợp lệ: Thiếu các trường cốt lõi của P0!');
    }
    return parsed as type GameSaveState;
  }

  public static saveToFile(filePath: string, state: type GameSaveState): void {
    const dir = path.dirname(filePath);
    if (!fs.existsSync(dir)) {
      fs.mkdirSync(dir, { recursive: true });
    }
    const json = this.serialize(state);
    fs.writeFileSync(filePath, json, 'utf-8');
  }

  public static loadFromFile(filePath: string): type GameSaveState {
    if (!fs.existsSync(filePath)) {
      throw new Error(`File save không tồn tại: ${filePath}`);
    }
    const content = fs.readFileSync(filePath, 'utf-8');
    return this.deserialize(content);
  }
}
