import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { Router } from '@angular/router';
import { LoginByEmailComponent } from './login-by-email.component';
import { ApiService } from '../../../services/api.service';
import { ErrorService } from '../../../services/error.service';
import { TokenStorageService } from '../../../services/token-storage.service';
import { InlineCountdownStubComponent } from '../inline-countdown/inline-countdown.component.stub';
import {
    GenerateLoginCodeResponseDto,
    LoginResponseDto,
} from '../../../api-client/api-client';

describe('LoginByEmailComponent', () => {
    let component: LoginByEmailComponent;
    let fixture: ComponentFixture<LoginByEmailComponent>;
    let apiService: MockedObject<ApiService>;
    let tokenStorageService: MockedObject<TokenStorageService>;
    let errorService: MockedObject<ErrorService>;
    let router: MockedObject<Router>;

    beforeEach(async () => {
        apiService = {
            loginByEmail: vi.fn().mockName('ApiService.loginByEmail'),
            generateLoginCode: vi.fn().mockName('ApiService.generateLoginCode'),
        } as MockedObject<ApiService>;
        apiService.loginByEmail.mockResolvedValue(
            new LoginResponseDto({ token: 'fakeToken' }),
        );
        apiService.generateLoginCode.mockResolvedValue(
            new GenerateLoginCodeResponseDto({ codeValidInSeconds: 60 }),
        );
        tokenStorageService = {
            saveToken: vi.fn().mockName('TokenStorageService.saveToken'),
        } as MockedObject<TokenStorageService>;
        errorService = {
            handleError: vi.fn().mockName('ErrorService.handleError'),
        } as MockedObject<ErrorService>;
        router = {
            navigate: vi.fn().mockName('Router.navigate'),
        } as MockedObject<Router>;

        await TestBed.configureTestingModule({
            declarations: [LoginByEmailComponent, InlineCountdownStubComponent],
            imports: [ReactiveFormsModule],
            providers: [
                FormBuilder,
                { provide: Router, useValue: router },
                { provide: ApiService, useValue: apiService },
                { provide: TokenStorageService, useValue: tokenStorageService },
                { provide: ErrorService, useValue: errorService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(LoginByEmailComponent);
        component = fixture.componentInstance;
    });

    it('should create the component', () => {
        expect(component).toBeTruthy();
    });

    it('should have a form with email and code controls', () => {
        // Arrange

        // Act

        // Assert
        expect(component.form.contains('email')).toBe(true);
        expect(component.form.contains('code')).toBe(true);
    });

    it('should make the email control required and validate email format', () => {
        // Arrange
        const emailControl = component.form.get('email');

        // Act
        emailControl.setValue('');

        // Assert
        expect(emailControl.valid).toBe(false);

        // Act
        emailControl.setValue('test@example.com');

        // Assert
        expect(emailControl.valid).toBe(true);
    });

    it('should make the code control required and validate code format', () => {
        // Arrange
        const codeControl = component.form.get('code');

        // Act
        codeControl.setValue('');

        // Assert
        expect(codeControl.valid).toBe(false);

        // Act
        codeControl.setValue('1234');

        // Assert
        expect(codeControl.valid).toBe(true);
    });

    it('should call onSubmit and handle code request correctly', async () => {
        // Arrange
        component.form.setValue({ email: 'test@example.com', code: '' });

        // Act
        await component.onSubmit();

        // Assert
        expect(apiService.generateLoginCode).toHaveBeenCalledWith(
            'test@example.com',
        );
        expect(component.isCodeRequested()).toBe(true);
        expect(component.isCodeRequestInProgress()).toBe(false);
        expect(component.codeValidInSeconds()).toBe(60);
    });

    it('should handle code request error correctly', async () => {
        // Arrange
        apiService.generateLoginCode.mockRejectedValue(new Error('error'));
        component.form.setValue({ email: 'test@example.com', code: '' });

        // Act
        await component.onSubmit();

        // Assert
        expect(errorService.handleError).toHaveBeenCalled();
        expect(component.isCodeRequestInProgress()).toBe(false);
    });

    it('should call submit and handle login response correctly', async () => {
        // Arrange
        component.form.setValue({ email: 'test@example.com', code: '1234' });
        router.navigate.mockResolvedValue(true);

        // Act
        await component.onSubmit();

        // Assert
        expect(apiService.loginByEmail).toHaveBeenCalledWith(
            'test@example.com',
            1234,
        );
        expect(tokenStorageService.saveToken).toHaveBeenCalledWith('fakeToken');
        expect(router.navigate).toHaveBeenCalledWith(['chats']);
        expect(component.isSubmitInProgress()).toBe(false);
    });

    it('should handle submit error correctly', async () => {
        // Arrange
        apiService.loginByEmail.mockRejectedValue(new Error('error'));
        component.form.setValue({ email: 'test@example.com', code: '1234' });

        // Act
        await component.onSubmit();

        // Assert
        expect(errorService.handleError).toHaveBeenCalled();
        expect(component.isSubmitInProgress()).toBe(false);
    });

    it('should navigate to chats on back button click', async () => {
        // Arrange
        router.navigate.mockResolvedValue(true);

        // Act
        await component.onBack();

        // Assert
        expect(router.navigate).toHaveBeenCalledWith(['chats']);
    });

    it('should set isCodeRequested to false when timer expires', () => {
        // Arrange

        // Act
        component.onTimerExpired();

        // Assert
        expect(component.isCodeRequested()).toBe(false);
    });

    it('should display the countdown timer if code is requested and valid in seconds is greater than 0', () => {
        // Arrange

        // Act
        component.isCodeRequested.set(true);
        component.codeValidInSeconds.set(60);
        fixture.detectChanges();

        // Assert
        const countdownElement = fixture.nativeElement.querySelector(
            'app-inline-countdown',
        );
        expect(countdownElement).toBeTruthy();
    });

    it('should call onSubmit when the code is valid and form is valid', () => {
        // Arrange
        vi.spyOn(component, 'onSubmit');
        component.form.get('email').setValue('test@example.com');
        component.form.get('code').setValue('1234');

        // Act
        component.onCodeChange();

        // Assert
        expect(component.onSubmit).toHaveBeenCalled();
    });

    it('should call onSubmit when valid code is entered and form is valid (input event raised)', () => {
        // Arrange
        vi.spyOn(component, 'onSubmit');
        component.isCodeRequested.set(true);
        component.form.get('email').setValue('test@example.com');
        fixture.detectChanges();

        const codeInput = fixture.nativeElement.querySelector('#code');

        // Act
        codeInput.value = '1234';
        codeInput.dispatchEvent(new Event('input'));

        // Assert
        expect(component.onSubmit).toHaveBeenCalled();
    });
});
