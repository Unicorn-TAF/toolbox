namespace Unicorn.ConsoleRunner
{
    public static class ResourceAsciiLogo
    {
        private const string LogoImage = @"
                     WNXK00OOOOOOOO0KKXNW               
                 WXKOkxxkkOO0000000OOkkkO0KNW           
              WX0kxxk0KXNWW         WWNXKOkkOKNW        
            WXOxdk0XW                    WNX0kk0XW      
          WXOddOXW NXN                      WN0kk0NW    
         WKxdkKW   WKO0XW   WNNNNNNNXXKKXNW   WN0kOKW   
        W0ddON      WKxdk0XWXOxxxkkO0X0xdkO0KXW WKOkKW  
       W0dd0W         Xkoodxxdooooood0X00KK0kxk0XNX0OKW 
       KdoON          WKdooooooooooook00OkOKNKOxxOKNXKX 
      NkoxX         WNOdooxdlloollooloooooodkXNKkddkKNNW
      KdoOW       WXOdlldOOdllllllllllllllllld0NNOolokXW
      0oo0W     WXOdllllooollllllllllllllllllclONW0l:;lK
      0oo0    WNOdlllllllllllllllllcccccccccccclkWWO:,,x
      KolOW   NxlllllllllllcloxOxlcccccccccc::::l0WXo,;k
      XxlxN   Kollcclldk0K000XWWXdcccccc:::::::;:dNXo,c0
      WOlo0W  N0dlcokKN         Nxccc::::::::;;;;l0O:,dN
       NxldKW   NKOKW           Nxc:::::::::;;;;;cdl,lK 
        Xxld0W                 WKo::::::::;;;;;;,;;;c0W 
         XxloON               X0dc::::::;;;;;;;;,,;lKW  
          NOold0N           WKdcc::::::;;;;;;;;,,:xX    
           WXOxdx0XW      WKxl::::::;;;;;;;;;;;:dKW     
              WNK0KNWWNX0kdc::::::;;;;;;;;;;;cxKW       
                 WXOxdocc:::::::::;;;;;;;:lx0NW         
                   WX0kxdolccc:::ccclodkOKN             
                       WWNK00OOOO00KXNW                 ";


        private const string LogoText = @"
,--. ,--. ,--.  ,--. ,--.  ,-----.  ,-----.  ,------.  ,--.  ,--.
|  | |  | |  ,'.|  | |  | '  .--./ '  .-.  ' |  .--. ' |  ,'.|  |
|  | |  | |  |' '  | |  | |  |     |  | |  | |  '--'.' |  |' '  |
'  '-'  ' |  | `   | |  | '  '--'\ '  '-'  ' |  |\  \  |  | `   |
 `-----'  `--'  `--' `--'  `-----'  `-----'  `--' '--' `--'  `--'";

        public const string Logo = LogoImage + "\n" + LogoText;
    }
}
