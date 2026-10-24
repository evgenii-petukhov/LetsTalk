import {
    ChangeDetectionStrategy,
    Component,
    computed,
    inject,
    signal
} from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { errorMessages } from '../../../constants/errors';
import { ApiService } from '../../../services/api.service';
import { ErrorService } from '../../../services/error.service';
import { TokenStorageService } from '../../../services/token-storage.service';

@Component({
    selector: 'app-login-by-email',
    templateUrl: './login-by-email.component.html',
    styleUrl: './login-by-email.component.scss',
    standalone: false,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginByEmailComponent {
    isCodeRequested = signal(false);
    isCodeRequestInProgress = signal(false);
    isSubmitInProgress = signal(false);
    codeValidInSeconds = signal(0);
    isCodeValidityShown = computed(
        () => this.isCodeRequested() && this.codeValidInSeconds() > 0,
    );
    isCountDownShown = computed(
        () => this.isCodeValidityShown() && !this.isSubmitInProgress(),
    );

    private codeExpiresAt = 0;

    private readonly router = inject(Router);
    private readonly fb = inject(FormBuilder);
    private readonly apiService = inject(ApiService);
    private readonly tokenStorage = inject(TokenStorageService);
    private readonly errorService = inject(ErrorService);

    form = this.fb.group({
        email: ['', [Validators.email, Validators.required]],
        code: ['', [Validators.pattern('\\s*\\d{4}\\s*'), Validators.required]],
    });

    async onSubmit(): Promise<void> {
        if (this.form.value.code) {
            await this.submit();
        } else {
            await this.requestCode();
        }
    }

    async onBack(): Promise<void> {
        await this.router.navigate(['chats']);
    }

    onTimerExpired(): void {
        this.isCodeRequested.set(false);
    }

    onCodeChange(): void {
        if (this.form.valid) {
            this.onSubmit();
        }
    }

    private async submit(): Promise<void> {
        this.isSubmitInProgress.set(true);
        try {
            const loginResponseDto = await this.apiService.loginByEmail(
                this.form.value.email,
                Number(this.form.value.code),
            );
            this.tokenStorage.saveToken(loginResponseDto.token);
            await this.router.navigate(['chats']);
        } catch (e) {
            this.errorService.handleError(e, errorMessages.generateCode);
            this.resumeCountdown();
        } finally {
            this.isSubmitInProgress.set(false);
        }
    }

    private async requestCode(): Promise<void> {
        this.isCodeRequestInProgress.set(true);
        try {
            const data = await this.apiService.generateLoginCode(
                this.form.value.email,
            );
            this.isCodeRequested.set(true);
            this.isCodeRequestInProgress.set(false);
            this.codeValidInSeconds.set(data.codeValidInSeconds);
            this.codeExpiresAt = Date.now() + data.codeValidInSeconds * 1000;
        } catch (e) {
            this.errorService.handleError(e, errorMessages.generateCode);
            this.isCodeRequestInProgress.set(false);
        }
    }

    // The countdown is destroyed while the code is being submitted and
    // restarts from startValue, so pass it the time that is actually left
    private resumeCountdown(): void {
        const secondsLeft = Math.ceil((this.codeExpiresAt - Date.now()) / 1000);
        if (secondsLeft > 0) {
            this.codeValidInSeconds.set(secondsLeft);
        } else {
            this.onTimerExpired();
        }
    }
}
