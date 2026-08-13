import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SharedService } from '../../../core/services/shared.service';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageModule } from 'primeng/message';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    PasswordModule,
    CheckboxModule,
    MessageModule,
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  public sharedService = inject(SharedService);

  isSubmitting = signal<boolean>(false);
  errorMessage = signal<string>('');
  currentYear = new Date().getFullYear();

  loginForm = this.fb.group({
    usernameOrEmail: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    rememberMe: [false],
  });

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    const model = this.loginForm.value as any;

    this.authService.login(model).subscribe({
      next: (res) => {
        const dto = res.data as any;
        if (res.success && dto?.succeeded && dto?.accessToken) {
          this.router.navigate(['/dashboard']);
        } else {
          this.errorMessage.set(dto?.message ?? res.message ?? 'Login failed. Please check your credentials.');
          this.isSubmitting.set(false);
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        const msg = err?.error?.data?.message ?? err?.error?.message ?? 'Invalid credentials. Please check your username and password.';
        this.errorMessage.set(msg);
        this.isSubmitting.set(false);
        this.cdr.detectChanges();
      },
    });
  }
}
