using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.ContextMenus;

namespace Server.Multis
{
    public class HouseSign : Item
	{
		private BaseHouse m_Owner;
		private Mobile m_OrgOwner;

		public HouseSign( BaseHouse owner ) : base( 0xBD2 )
		{
			m_Owner = owner;
			m_OrgOwner = m_Owner.Owner;
			Movable = false;
		}

		public HouseSign( Serial serial ) : base( serial )
		{
		}

		public string GetName()
		{
			if ( Name == null )
				return "An Unnamed House";

			return Name;
		}

		public BaseHouse Owner
		{
			get
			{
				return m_Owner;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool RestrictDecay
		{
			get{ return ( m_Owner != null && m_Owner.RestrictDecay ); }
			set{ if ( m_Owner != null ) m_Owner.RestrictDecay = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile OriginalOwner
		{
			get
			{
				return m_OrgOwner;
			}
		}

		public override void OnAfterDelete()
		{
			base.OnAfterDelete();

			if ( m_Owner != null && !m_Owner.Deleted )
				m_Owner.Delete();
		}

		public override void OnSingleClick( Mobile from )
		{
			if ( m_Owner != null && BaseHouse.DecayEnabled && m_Owner.DecayPeriod != TimeSpan.Zero )
			{
				string message;

				switch ( m_Owner.DecayLevel )
				{
					case DecayLevel.Ageless:	message = "ageless"; break;
					case DecayLevel.Fairly:		message = "fairly worn"; break;
					case DecayLevel.Greatly:	message = "greatly worn"; break;
					case DecayLevel.LikeNew:	message = "like new"; break;
					case DecayLevel.Slightly:	message = "slightly worn"; break;
					case DecayLevel.Somewhat:	message = "somewhat worn"; break;
					default:					message = "in danger of collapsing"; break;
				}

				LabelTo( from, "This house is {0}.", message );
			}

			base.OnSingleClick( from );
		}

		public void ShowSign( Mobile m )
		{
			if ( m_Owner != null )
			{
				if ( m_Owner.IsFriend( m ) && m.AccessLevel < AccessLevel.GameMaster )
				{
					m_Owner.RefreshDecay();
                    m.SendLocalizedMessage( 501293 ); // Welcome back to the house, friend!
				}

				m.SendGump( new HouseGump( m, m_Owner ) );
			}
		}

		public void ClaimGump_Callback( Mobile from, bool okay, object state )
		{
			if ( okay && m_Owner != null && m_Owner.Owner == null && m_Owner.DecayLevel != DecayLevel.DemolitionPending )
			{
				bool canClaim = false;

				if ( m_Owner.CoOwners == null || m_Owner.CoOwners.Count == 0 )
					canClaim = m_Owner.IsFriend( from );
				else
					canClaim = m_Owner.IsCoOwner( from );

				if ( canClaim && !BaseHouse.HasAccountHouse( from ) )
				{
					m_Owner.Owner = from;
				}
			}

			ShowSign( from );
		}

		public override void OnDoubleClick( Mobile m )
		{
			if ( m_Owner == null )
				return;

			if ( m.AccessLevel < AccessLevel.GameMaster && m_Owner.Owner == null && m_Owner.DecayLevel != DecayLevel.DemolitionPending )
			{
				bool canClaim = false;

				if ( m_Owner.CoOwners == null || m_Owner.CoOwners.Count == 0 )
					canClaim = m_Owner.IsFriend( m );
				else
					canClaim = m_Owner.IsCoOwner( m );

				if ( canClaim && !BaseHouse.HasAccountHouse( m ) )
				{
					/* You do not currently own any house on any shard with this account,
					 * and this house currently does not have an owner.  If you wish, you
					 * may choose to claim this house and become its rightful owner.  If
					 * you do this, it will become your Primary house and automatically
					 * refresh.  If you claim this house, you will be unable to place
					 * another house or have another house transferred to you for the
					 * next 7 days.  Do you wish to claim this house?
					 */
					m.SendGump( new WarningGump( 501036, 32512, 1049719, 32512, 420, 280, new WarningGumpCallback( ClaimGump_Callback ), null ) );
				}
			}

			ShowSign( m );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( m_Owner );
			writer.Write( m_OrgOwner );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Owner = reader.ReadItem() as BaseHouse;
					m_OrgOwner = reader.ReadMobile();

					break;
				}
			}

			if ( this.Name == "a house sign" )
				this.Name = null;
		}
	}
}