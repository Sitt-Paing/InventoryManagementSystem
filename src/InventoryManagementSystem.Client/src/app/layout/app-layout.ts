import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { AppHeader } from './header/app-header';
import { AppSidebar } from './sidebar/app-sidebar';
import { SharedService } from '../core/services/shared.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, AppHeader, AppSidebar],
  templateUrl: './app-layout.html',
  styleUrl: './app-layout.scss'
})
export class AppLayout {
  constructor(public sharedService: SharedService) {}
}
