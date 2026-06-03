import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SharedModule } from '../shared/shared.module';
import { SidebarComponent } from './sidebar.component';
import { ChatSidebarModule } from './chat-sidebar/chat-sidebar.module';
import { AccountSidebarModule } from './account-sidebar/account-sidebar.module';

@NgModule({
    declarations: [
        SidebarComponent,
    ],
    imports: [
        CommonModule,
        SharedModule,
        ChatSidebarModule,
        AccountSidebarModule,
    ],
    exports: [SidebarComponent],
})
export class SidebarModule {}
