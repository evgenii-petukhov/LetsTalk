import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SignInComponent } from './components/sign-in/sign-in.component';
import { MessengerComponent } from './components/messenger/messenger.component';
import { ProfileComponent } from './components/profile/profile.component';
import { authenticatedOnlyGuard } from './guards/authenticated-only-guard';
import { completeProfileGuard } from './guards/complete-profile-guard';
import { PrivacyPolicyComponent } from './components/privacy-policy/privacy-policy.component';
import { LoginByEmailComponent } from './components/sign-in/login-by-email/login-by-email.component';
import { anonymousOnlyGuard } from './guards/anonymous-only-guard';
import { ChatComponent } from './components/chat/chat.component';
import { ImageViewerComponent } from './components/image-viewer/image-viewer.component';

const routes: Routes = [
    {
        path: '',
        redirectTo: '/messenger',
        pathMatch: 'full',
    },
    {
        path: 'sign-in',
        component: SignInComponent,
        canActivate: [anonymousOnlyGuard],
    },
    {
        path: 'login-by-email',
        component: LoginByEmailComponent,
        canActivate: [anonymousOnlyGuard],
    },
    {
        path: 'privacy-policy',
        component: PrivacyPolicyComponent,
    },
    {
        path: 'messenger',
        component: MessengerComponent,
        canActivate: [authenticatedOnlyGuard, completeProfileGuard],
        children: [
            {
                path: 'chat/:id',
                component: ChatComponent,
                canActivate: [authenticatedOnlyGuard, completeProfileGuard],
                children: [
                    {
                        path: 'image/:imageKey',
                        component: ImageViewerComponent,
                        canActivate: [authenticatedOnlyGuard, completeProfileGuard],
                    },
                ],
            },
        ],
    },
    {
        path: 'profile',
        component: ProfileComponent,
        canActivate: [authenticatedOnlyGuard],
    },
    {
        path: '**',
        redirectTo: '/messenger',
    },
];

@NgModule({
    imports: [RouterModule.forRoot(routes)],
    exports: [RouterModule],
})
export class AppRoutingModule {}
