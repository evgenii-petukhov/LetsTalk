import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-user-details',
    template: '<div></div>',
    standalone: false,
})
export class UserDetailsStubComponent {
    @Input() value: string;
}
