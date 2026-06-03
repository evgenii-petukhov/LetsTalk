import { Component, EventEmitter, Output } from '@angular/core';

@Component({
    selector: 'app-logout-button',
    template: '<div></div>',
    standalone: false,
})
export class LogoutButtonStubComponent {
    @Output() buttonClick = new EventEmitter();
}
