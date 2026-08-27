import { Injectable, signal, computed } from '@angular/core';
import { AVAILABLE_LANGUAGES, DEFAULT_LANGUAGE, LanguageCode, LanguageOption } from '../i18n/languages';
import { EN_DICTIONARY } from '../i18n/en';
import { MM_DICTIONARY } from '../i18n/mm';

@Injectable({
  providedIn: 'root'
})
export class TranslationService {
  private readonly STORAGE_KEY = 'app_language';
  
  // Available language definitions
  public readonly languages: LanguageOption[] = AVAILABLE_LANGUAGES;

  // Dictionaries lookup table
  private dictionaries: Record<LanguageCode, Record<string, any>> = {
    en: EN_DICTIONARY,
    mm: MM_DICTIONARY
  };

  // Signal holding the active language code
  public currentLanguage = signal<LanguageCode>(this.loadInitialLanguage());

  // Computed active language option object
  public currentLanguageOption = computed(() => {
    const code = this.currentLanguage();
    return this.languages.find(l => l.code === code) || this.languages[0];
  });

  constructor() {
    // Sync html lang attribute
    this.updateHtmlLang(this.currentLanguage());
  }

  /**
   * Returns initial language from localStorage or default
   */
  private loadInitialLanguage(): LanguageCode {
    if (typeof localStorage === 'undefined') return DEFAULT_LANGUAGE;
    const saved = localStorage.getItem(this.STORAGE_KEY) as LanguageCode;
    if (saved && (saved === 'en' || saved === 'mm')) {
      return saved;
    }
    return DEFAULT_LANGUAGE;
  }

  /**
   * Switch the active language and save preference to localStorage
   */
  public setLanguage(lang: LanguageCode): void {
    if (lang !== this.currentLanguage()) {
      this.currentLanguage.set(lang);
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(this.STORAGE_KEY, lang);
      }
      this.updateHtmlLang(lang);
    }
  }

  /**
   * Toggle between EN and MM
   */
  public toggleLanguage(): void {
    const nextLang: LanguageCode = this.currentLanguage() === 'en' ? 'mm' : 'en';
    this.setLanguage(nextLang);
  }

  /**
   * Synchronously translate a key into the active language
   * Usage: translate('NAV.DASHBOARD') or translate('COMMON.WELCOME', { name: 'Admin' })
   */
  public translate(key: string, params?: Record<string, any>): string {
    if (!key) return '';

    const lang = this.currentLanguage();
    const dictionary = this.dictionaries[lang] || this.dictionaries[DEFAULT_LANGUAGE];

    // Traverse dot notation
    const keys = key.split('.');
    let value: any = dictionary;

    for (const k of keys) {
      if (value && typeof value === 'object' && k in value) {
        value = value[k];
      } else {
        // Fallback to English dictionary if key missing in MM
        value = this.getFallbackTranslation(keys);
        break;
      }
    }

    if (typeof value !== 'string') {
      return key; // return key as fallback if not found
    }

    // Interpolate params e.g. {name}
    if (params) {
      return Object.keys(params).reduce((str, paramKey) => {
        return str.replace(new RegExp(`\\{${paramKey}\\}`, 'g'), String(params[paramKey]));
      }, value);
    }

    return value;
  }

  public instant(key: string, params?: Record<string, any>): string {
    return this.translate(key, params);
  }

  private getFallbackTranslation(keys: string[]): string | null {
    let value: any = this.dictionaries[DEFAULT_LANGUAGE];
    for (const k of keys) {
      if (value && typeof value === 'object' && k in value) {
        value = value[k];
      } else {
        return null;
      }
    }
    return typeof value === 'string' ? value : null;
  }

  private updateHtmlLang(lang: LanguageCode): void {
    if (typeof document !== 'undefined' && document.documentElement) {
      document.documentElement.lang = lang;
      if (lang === 'mm') {
        document.documentElement.classList.add('lang-mm');
      } else {
        document.documentElement.classList.remove('lang-mm');
      }
    }
  }
}
