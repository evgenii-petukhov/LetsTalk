import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AccountListItemComponent } from './account-list-item/account-list-item.component';
import { SharedModule } from '../../shared/shared.module';
import { AccountListComponent } from './account-list/account-list.component';
import { AccountListTopPanelComponent } from './account-list-top-panel/account-list-top-panel.component';
import { AccountSidebarComponent } from './account-sidebar.component';

@NgModule({
    declarations: [
        AccountListItemComponent,
        AccountListComponent,
        AccountListTopPanelComponent,
        AccountSidebarComponent,
    ],
    imports: [CommonModule, SharedModule],
    exports: [AccountSidebarComponent],
})
export class AccountSidebarModule {}
