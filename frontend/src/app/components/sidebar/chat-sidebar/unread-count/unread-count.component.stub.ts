import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-unread-count',
    template: '<div></div>',
    standalone: false,
})
export class UnreadCountStubComponent {
    @Input() value: number;
}
