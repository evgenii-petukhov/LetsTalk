import {
    provideHttpClient,
    withInterceptorsFromDi,
} from '@angular/common/http';
import { NgModule } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { ProfileComponent } from './components/profile/profile.component';
import { environment } from '../environments/environment';
import { authInterceptorProvider } from './providers/auth-interceptor-provider';
import { API_BASE_URL } from './api-client/api-client';
import { apiClientProvider } from './providers/api-client-provider';
import { MessengerComponent } from './components/messenger/messenger.component';
import { BrowserModule } from '@angular/platform-browser';
import { ToastrModule } from 'ngx-toastr';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { StoreModule } from '@ngrx/store';
import { storeConfig } from './state/store-config';
import { ReactiveFormsModule } from '@angular/forms';
import { PrivacyPolicyComponent } from './components/privacy-policy/privacy-policy.component';
import { ImageViewerComponent } from './components/image-viewer/image-viewer.component';
import { httpLogInterceptorProvider } from './providers/http-log-interceptor-provider';
import { SharedModule } from './components/shared/shared.module';
import { SidebarModule } from './components/sidebar/sidebar.module';
import { SignInModule } from './components/sign-in/sign-in.module';
import { ChatModule } from './components/chat/chat.module';

@NgModule({
    declarations: [
        AppComponent,
        ProfileComponent,
        MessengerComponent,
        PrivacyPolicyComponent,
        ImageViewerComponent,
    ],
    imports: [
        BrowserModule,
        AppRoutingModule,
        FontAwesomeModule,
        BrowserAnimationsModule,
        ToastrModule.forRoot(),
        StoreModule.forRoot(storeConfig),
        ReactiveFormsModule,
        SharedModule,
        SidebarModule,
        SignInModule,
        ChatModule,
    ],
    providers: [
        authInterceptorProvider,
        httpLogInterceptorProvider,
        apiClientProvider,
        provideHttpClient(withInterceptorsFromDi()),
        {
            provide: API_BASE_URL,
            useFactory: () => environment.services.api.url,
        },
    ],
    bootstrap: [AppComponent],
})
export class AppModule {}
