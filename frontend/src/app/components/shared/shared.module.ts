import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { BackButtonComponent } from './back-button/back-button.component';
import { AvatarComponent } from './avatar/avatar.component';
import { UserDetailsComponent } from './user-details/user-details.component';
import { OrderByPipe } from '../../pipes/orderby';
import { TopPanelComponent } from './top-panel/top-panel.component';

@NgModule({
    declarations: [
        BackButtonComponent,
        AvatarComponent,
        UserDetailsComponent,
        TopPanelComponent,
        OrderByPipe,
    ],
    imports: [CommonModule, FontAwesomeModule],
    exports: [
        BackButtonComponent,
        AvatarComponent,
        UserDetailsComponent,
        TopPanelComponent,
        OrderByPipe,
    ],
})
export class SharedModule {}
