import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InlineCountdownComponent } from './inline-countdown/inline-countdown.component';
import { SignInComponent } from './sign-in.component';
import { LoginByEmailComponent } from './login-by-email/login-by-email.component';
import { AppRoutingModule } from '../../app-routing.module';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { ReactiveFormsModule } from '@angular/forms';

@NgModule({
    declarations: [
        InlineCountdownComponent,
        SignInComponent,
        LoginByEmailComponent,
    ],
    imports: [
        CommonModule,
        AppRoutingModule,
        FontAwesomeModule,
        ReactiveFormsModule,
    ],
})
export class SignInModule {}
