import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslationService } from '../services/translation.service';

@Pipe({
  name: 'translate',
  standalone: true,
  pure: false
})
export class TranslatePipe implements PipeTransform {
  private translationService = inject(TranslationService);

  transform(key: string, params?: Record<string, any>): string {
    if (!key) return '';
    // Reading currentLanguage signal registers reactivity
    this.translationService.currentLanguage();
    return this.translationService.translate(key, params);
  }
}
