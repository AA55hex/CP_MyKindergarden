//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MyKindergarden
{
    using System;
    using System.Collections.Generic;
    
    public partial class KinderGroup
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public KinderGroup()
        {
            this.Children = new HashSet<Child>();
            this.Clients = new HashSet<Client>();
        }
    
        public int Id { get; set; }
        public string GroupName { get; set; }
        public int GroupType_Id { get; set; }
        public bool IsActive { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Child> Children { get; set; }
        public virtual GroupType GroupType { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Client> Clients { get; set; }
    }
}
