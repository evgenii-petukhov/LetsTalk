import { Component, EventEmitter, Input, Output } from '@angular/core';
import { IAccountDto } from '../../../../api-client/api-client';

@Component({
    selector: 'app-account-list-item',
    templateUrl: './account-list-item.component.html',
    styleUrls: ['./account-list-item.component.scss'],
    standalone: false,
})
export class AccountListItemComponent {
    @Input() account: IAccountDto;
    @Output() accountSelected = new EventEmitter<IAccountDto>();

    onAccountSelected(): boolean {
        this.accountSelected.emit(this.account);
        return false;
    }
}
