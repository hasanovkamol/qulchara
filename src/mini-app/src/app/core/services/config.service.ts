import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { DOCUMENT } from '@angular/common';

export interface AppConfig {
  apiUrl: string;
}

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  private config!: AppConfig;
  private http = inject(HttpClient);
  private document = inject(DOCUMENT);

  async load(): Promise<void> {
    // document.baseURI ensures correct path with any base-href
    // localhost: http://localhost:4200/config.json
    // GitHub Pages: https://hasanovkamol.github.io/qulchara/config.json
    const baseUrl = this.document.baseURI;
    this.config = await firstValueFrom(
      this.http.get<AppConfig>(`${baseUrl}config.json`)
    );
  }

  get apiUrl(): string {
    return this.config?.apiUrl ?? 'http://localhost:4041/api';
  }
}
