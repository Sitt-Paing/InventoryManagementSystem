export type LanguageCode = 'en' | 'mm';

export interface LanguageOption {
  code: LanguageCode;
  label: string;
  nativeLabel: string;
  shortLabel: string;
  flag: string;
  flagIcon: string;
}

export const AVAILABLE_LANGUAGES: LanguageOption[] = [
  {
    code: 'en',
    label: 'English',
    nativeLabel: 'English',
    shortLabel: 'English',
    flag: '🇺🇸',
    flagIcon: 'flags/en.svg'
  },
  {
    code: 'mm',
    label: 'Myanmar',
    nativeLabel: 'မြန်မာ (Myanmar)',
    shortLabel: 'မြန်မာ',
    flag: '🇲🇲',
    flagIcon: 'flags/mm.svg'
  }
];

export const DEFAULT_LANGUAGE: LanguageCode = 'en';
