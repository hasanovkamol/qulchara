import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class TelegramService {
  private tg = (window as any).Telegram?.WebApp;

  constructor() {
    if (this.tg) {
      this.tg.ready();
      this.tg.expand();
    }
  }

  getInitData(): string {
    // For local testing, return a dummy string if not in Telegram
    if (!this.tg || !this.tg.initData) {
      return "query_id=test&user=%7B%22id%22%3A12345%2C%22first_name%22%3A%22Test%22%2C%22last_name%22%3A%22User%22%2C%22username%22%3A%22testuser%22%7D&hash=test";
    }
    return this.tg.initData;
  }

  close() {
    this.tg?.close();
  }
}
