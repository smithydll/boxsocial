erase /F /Q "Package"

md "%1"Package\applications

copy "%1"Blog\bin\%2\Blog.dll "%1"Package\
copy "%1"Blog\bin\%2\Blog.dll "%1"Package\applications\
copy "%1"Blog\bin\%2\Blog.pdb "%1"Package\
copy "%1"Blog\languages\Blog.en.resources "%1"Package\

copy "%1"GuestBook\bin\%2\GuestBook.dll "%1"Package\
copy "%1"GuestBook\bin\%2\GuestBook.dll "%1"Package\applications\
copy "%1"GuestBook\bin\%2\GuestBook.pdb "%1"Package\
copy "%1"GuestBook\languages\GuestBook.en.resources "%1"Package\

copy "%1"Profile\bin\%2\Profile.dll "%1"Package\
copy "%1"Profile\bin\%2\Profile.dll "%1"Package\applications\
copy "%1"Profile\bin\%2\Profile.pdb "%1"Package\
copy "%1"Profile\languages\Profile.en.resources "%1"Package\

copy "%1"Calendar\bin\%2\Calendar.dll "%1"Package\
copy "%1"Calendar\bin\%2\Calendar.dll "%1"Package\applications\
copy "%1"Calendar\bin\%2\Calendar.pdb "%1"Package\
copy "%1"Calendar\languages\Calendar.en.resources "%1"Package\

copy "%1"Gallery\bin\%2\Gallery.dll "%1"Package\
copy "%1"Gallery\bin\%2\Gallery.dll "%1"Package\applications\
copy "%1"Gallery\bin\%2\Gallery.pdb "%1"Package\
copy "%1"Gallery\languages\Gallery.en.resources "%1"Package\

copy "%1"Pages\bin\%2\Pages.dll "%1"Package\
copy "%1"Pages\bin\%2\Pages.dll "%1"Package\applications\
copy "%1"Pages\bin\%2\Pages.pdb "%1"Package\
copy "%1"Pages\languages\Pages.en.resources "%1"Package\

copy "%1"Forum\bin\%2\Forum.dll "%1"Package\
copy "%1"Forum\bin\%2\Forum.dll "%1"Package\applications\
copy "%1"Forum\bin\%2\Forum.pdb "%1"Package\
copy "%1"Forum\languages\Forum.en.resources "%1"Package\

copy "%1"News\bin\%2\News.dll "%1"Package\
copy "%1"News\bin\%2\News.dll "%1"Package\applications\
copy "%1"News\bin\%2\News.pdb "%1"Package\
copy "%1"News\languages\News.en.resources "%1"Package\

copy "%1"Mail\bin\%2\Mail.dll "%1"Package\
copy "%1"Mail\bin\%2\Mail.dll "%1"Package\applications\
copy "%1"Mail\bin\%2\Mail.pdb "%1"Package\
copy "%1"Mail\languages\Mail.en.resources "%1"Package\

copy "%1"EnterpriseResourcePlanning\bin\%2\EnterpriseResourcePlanning.dll "%1"Package\
copy "%1"EnterpriseResourcePlanning\bin\%2\EnterpriseResourcePlanning.dll "%1"Package\applications\
copy "%1"EnterpriseResourcePlanning\bin\%2\EnterpriseResourcePlanning.pdb "%1"Package\
copy "%1"EnterpriseResourcePlanning\languages\EnterpriseResourcePlanning.en.resources "%1"Package\

copy "%1"BoxSocial.Forms\bin\%2\BoxSocial.Forms.dll "%1"Package\
copy "%1"BoxSocial.Forms\bin\%2\BoxSocial.Forms.pdb "%1"Package\
copy "%1"BoxSocial.KnowledgeBase\bin\%2\BoxSocial.KnowledgeBase.dll "%1"Package\
copy "%1"BoxSocial.KnowledgeBase\bin\%2\BoxSocial.KnowledgeBase.pdb "%1"Package\
copy "%1"BoxSocial.IO\bin\%2\BoxSocial.IO.dll "%1"Package\
copy "%1"BoxSocial.IO\bin\%2\BoxSocial.IO.pdb "%1"Package\
copy "%1"Groups\bin\%2\Groups.dll "%1"Package\
copy "%1"Groups\bin\%2\Groups.pdb "%1"Package\
copy "%1"Groups\languages\Groups.en.resources "%1"Package\
copy "%1"Networks\bin\%2\Networks.dll "%1"Package\
copy "%1"Networks\bin\%2\Networks.pdb "%1"Package\
copy "%1"Musician\bin\%2\Musician.dll "%1"Package\
copy "%1"Musician\bin\%2\Musician.pdb "%1"Package\
copy "%1"Musician\languages\Musician.en.resources "%1"Package\
copy "%1"BoxSocial.Internals\bin\%2\BoxSocial.Internals.dll "%1"Package\
copy "%1"BoxSocial.Internals\bin\%2\BoxSocial.Internals.pdb "%1"Package\
copy "%1"BoxSocial.Internals\languages\Internals.en.resources "%1"Package\

copy "%1"BoxSocial.FrontEnd\bin\%2\BoxSocial.FrontEnd.dll "%1"Package\
copy "%1"BoxSocial.FrontEnd\bin\%2\BoxSocial.FrontEnd.pdb "%1"Package\

copy "%1"BoxSocial\bin\MySql.Data.dll "%1"Package\

copy "%1"BoxSocial.Install\bin\%2\BoxSocial.Install.exe "%1"Package\
copy "%1"BoxSocial.Install\bin\%2\BoxSocial.Install.pdb "%1"Package\

md "%1"Package\GDK

copy "%1"GDK\*.svg "%1"Package\GDK

md "%1"Package\templates

copy "%1"BoxSocial\templates "%1"Package\templates

md "%1"Package\templates\emails

copy "%1"BoxSocial\templates\emails "%1"Package\templates\emails

md "%1"Package\templates\mobile

copy "%1"BoxSocial\templates\mobile "%1"Package\templates\mobile

md "%1"Package\templates\tablet

copy "%1"BoxSocial\templates\tablet "%1"Package\templates\tablet

md "%1"Package\styles

copy "%1"BoxSocial\styles "%1"Package\styles

md "%1"Package\styles\images

copy "%1"BoxSocial\styles\images "%1"Package\styles\images

md "%1"Package\scripts

copy "%1"BoxSocial\scripts "%1"Package\scripts

md "%1"Package\www

copy "%1"BoxSocial "%1"Package\www